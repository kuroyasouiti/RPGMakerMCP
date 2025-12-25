"""
Integration tests for error handling scenarios in RPGMaker MCP Server.

These tests verify that the server handles various error conditions gracefully,
including connection failures, timeouts, and invalid inputs.
"""

from __future__ import annotations

import asyncio
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from config.constants import mask_token, network, retry, security


class TestTokenMasking:
    """Tests for token masking functionality."""

    def test_mask_token_none(self):
        """Test masking None token."""
        assert mask_token(None) == "not set"

    def test_mask_token_empty(self):
        """Test masking empty token."""
        assert mask_token("") == "not set"

    def test_mask_token_short(self):
        """Test masking token shorter than visible chars."""
        assert mask_token("abc") == "set"

    def test_mask_token_exact_length(self):
        """Test masking token exactly at visible chars length."""
        token = "a" * security.TOKEN_VISIBLE_CHARS
        assert mask_token(token) == "set"

    def test_mask_token_long(self):
        """Test masking token longer than visible chars."""
        token = "secrettoken12345"
        result = mask_token(token)
        assert result.startswith("****")
        assert result.endswith(token[-security.TOKEN_VISIBLE_CHARS:])
        assert "secret" not in result


class TestNetworkConstants:
    """Tests for network configuration constants."""

    def test_default_timeout_is_positive(self):
        """Verify default timeout is a positive value."""
        assert network.DEFAULT_COMMAND_TIMEOUT_MS > 0

    def test_websocket_timeouts_are_positive(self):
        """Verify WebSocket timeouts are positive."""
        assert network.WEBSOCKET_OPEN_TIMEOUT > 0
        assert network.WEBSOCKET_CLOSE_TIMEOUT > 0

    def test_max_message_size_is_reasonable(self):
        """Verify max message size is at least 1MB."""
        assert network.MAX_MESSAGE_SIZE >= 1024 * 1024

    def test_max_ping_failures_is_positive(self):
        """Verify max ping failures is positive."""
        assert network.MAX_PING_FAILURES > 0


class TestRetryConstants:
    """Tests for retry configuration constants."""

    def test_quick_retry_attempts_is_positive(self):
        """Verify quick retry attempts is positive."""
        assert retry.QUICK_RETRY_ATTEMPTS > 0

    def test_backoff_base_delay_is_positive(self):
        """Verify backoff base delay is positive."""
        assert retry.BACKOFF_BASE_DELAY > 0

    def test_min_retry_delay_is_positive(self):
        """Verify minimum retry delay is positive."""
        assert retry.MIN_RETRY_DELAY > 0

    def test_exponential_backoff_calculation(self):
        """Verify exponential backoff calculation."""
        # First attempt: 0.5s
        delay1 = retry.BACKOFF_BASE_DELAY * (2 ** 0)
        assert delay1 == 0.5

        # Second attempt: 1.0s
        delay2 = retry.BACKOFF_BASE_DELAY * (2 ** 1)
        assert delay2 == 1.0

        # Third attempt: 2.0s
        delay3 = retry.BACKOFF_BASE_DELAY * (2 ** 2)
        assert delay3 == 2.0


class TestBridgeConnectionErrors:
    """Tests for bridge connection error handling."""

    @pytest.mark.asyncio
    async def test_connection_refused_is_handled(self):
        """Test that ConnectionRefusedError is caught and logged."""
        from bridge.bridge_connector import BridgeConnector

        connector = BridgeConnector()

        with patch("bridge.bridge_connector.websockets.connect") as mock_connect:
            mock_connect.side_effect = ConnectionRefusedError("Connection refused")

            # Start connector and let it try once
            connector.start()
            await asyncio.sleep(0.1)
            await connector.stop()

        # Connector should not crash
        assert connector._task is None

    @pytest.mark.asyncio
    async def test_timeout_error_is_handled(self):
        """Test that TimeoutError during connection is handled."""
        from bridge.bridge_connector import BridgeConnector

        connector = BridgeConnector()

        with patch("bridge.bridge_connector.websockets.connect") as mock_connect:
            mock_connect.side_effect = asyncio.TimeoutError("Connection timeout")

            connector.start()
            await asyncio.sleep(0.1)
            await connector.stop()

        assert connector._task is None

    @pytest.mark.asyncio
    async def test_os_error_is_handled(self):
        """Test that OSError (network unreachable) is handled."""
        from bridge.bridge_connector import BridgeConnector

        connector = BridgeConnector()

        with patch("bridge.bridge_connector.websockets.connect") as mock_connect:
            mock_connect.side_effect = OSError("Network unreachable")

            connector.start()
            await asyncio.sleep(0.1)
            await connector.stop()

        assert connector._task is None


class TestBridgeManagerErrors:
    """Tests for bridge manager error handling."""

    @pytest.mark.asyncio
    async def test_send_command_without_connection_raises(self):
        """Test that sending command without connection raises error."""
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        with pytest.raises(RuntimeError, match="Unity bridge is not connected"):
            await manager.send_command("test_tool", {})

    @pytest.mark.asyncio
    async def test_await_compilation_without_connection_raises(self):
        """Test that awaiting compilation without connection raises error."""
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        with pytest.raises(RuntimeError, match="Unity bridge is not connected"):
            await manager.await_compilation(timeout_seconds=1)

    @pytest.mark.asyncio
    async def test_command_timeout_raises_timeout_error(self):
        """Test that command timeout raises TimeoutError."""
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        # Mock socket as connected
        mock_socket = MagicMock()
        mock_socket.state = MagicMock()
        mock_socket.send = AsyncMock()
        manager._socket = mock_socket

        # Set very short timeout
        with pytest.raises(TimeoutError, match="timed out"):
            await manager.send_command("test_tool", {}, timeout_ms=10)

    @pytest.mark.asyncio
    async def test_compilation_timeout_raises_timeout_error(self):
        """Test that compilation timeout raises TimeoutError with helpful message."""
        from bridge.bridge_manager import BridgeManager

        manager = BridgeManager()

        # Mock socket as connected
        mock_socket = MagicMock()
        mock_socket.state = MagicMock()
        manager._socket = mock_socket

        with pytest.raises(TimeoutError) as exc_info:
            await manager.await_compilation(timeout_seconds=1)

        # Verify helpful error message
        assert "did not complete" in str(exc_info.value)
        assert "1 seconds" in str(exc_info.value)


class TestMainErrorHandling:
    """Tests for main module error handling."""

    @pytest.mark.asyncio
    async def test_invalid_json_returns_400(self):
        """Test that invalid JSON in bridge command returns 400."""
        from starlette.testclient import TestClient

        from main import app

        client = TestClient(app, raise_server_exceptions=False)

        response = client.post(
            "/bridge/command",
            content="invalid json{",
            headers={"Content-Type": "application/json"},
        )

        assert response.status_code == 400
        assert "Invalid JSON" in response.json()["error"]

    @pytest.mark.asyncio
    async def test_missing_tool_name_returns_400(self):
        """Test that missing toolName returns 400."""
        from starlette.testclient import TestClient

        from main import app

        client = TestClient(app, raise_server_exceptions=False)

        response = client.post("/bridge/command", json={"payload": {}})

        assert response.status_code == 400
        assert "toolName" in response.json()["error"]

    @pytest.mark.asyncio
    async def test_bridge_not_connected_returns_503(self):
        """Test that bridge not connected returns 503."""
        from starlette.testclient import TestClient

        from main import app

        client = TestClient(app, raise_server_exceptions=False)

        response = client.post(
            "/bridge/command", json={"toolName": "test", "payload": {}}
        )

        assert response.status_code == 503
        assert "not connected" in response.json()["error"]


class TestPathValidation:
    """Tests for path validation in handlers (C# side would have similar tests)."""

    def test_path_traversal_detection(self):
        """Test that path traversal attempts are detected."""
        # This simulates what the C# McpBridgeConstants.ValidateAndNormalizePath does
        import os
        from pathlib import Path

        def is_path_within_project(path: str, root: str) -> bool:
            try:
                full_path = os.path.abspath(path)
                normalized_root = os.path.abspath(root)
                return full_path.startswith(normalized_root)
            except Exception:
                return False

        project_root = "/fake/project"

        # Valid paths
        assert is_path_within_project("/fake/project/Assets", project_root)
        assert is_path_within_project("/fake/project/backup", project_root)

        # Invalid paths (path traversal)
        assert not is_path_within_project("/fake/other", project_root)
        assert not is_path_within_project("/etc/passwd", project_root)

    def test_empty_path_is_invalid(self):
        """Test that empty path is rejected."""

        def validate_path(path: str) -> bool:
            return bool(path and path.strip())

        assert not validate_path("")
        assert not validate_path("   ")
        assert not validate_path(None)  # type: ignore
        assert validate_path("/valid/path")


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
