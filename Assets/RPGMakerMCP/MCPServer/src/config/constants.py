"""
Configuration constants for RPGMaker MCP Server.

This module centralizes magic numbers and configuration values
that were previously hard-coded throughout the codebase.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Final


# =============================================================================
# Network Configuration
# =============================================================================

@dataclass(frozen=True)
class NetworkConfig:
    """Network-related configuration constants."""

    # Default timeout for bridge commands (milliseconds)
    DEFAULT_COMMAND_TIMEOUT_MS: Final[int] = 30_000

    # WebSocket connection timeouts (seconds)
    WEBSOCKET_OPEN_TIMEOUT: Final[float] = 10.0
    WEBSOCKET_CLOSE_TIMEOUT: Final[float] = 10.0

    # Maximum message size for WebSocket (bytes)
    MAX_MESSAGE_SIZE: Final[int] = 10 * 1024 * 1024  # 10MB

    # Ping configuration
    MAX_PING_FAILURES: Final[int] = 3


# =============================================================================
# Retry Configuration
# =============================================================================

@dataclass(frozen=True)
class RetryConfig:
    """Retry and backoff configuration constants."""

    # Number of quick retry attempts before switching to configured delay
    QUICK_RETRY_ATTEMPTS: Final[int] = 3

    # Base delay for exponential backoff (seconds)
    BACKOFF_BASE_DELAY: Final[float] = 0.5

    # Minimum delay between retries (seconds)
    MIN_RETRY_DELAY: Final[float] = 1.0


# =============================================================================
# Token Security
# =============================================================================

@dataclass(frozen=True)
class SecurityConfig:
    """Security-related configuration constants."""

    # Number of characters to show at the end of masked tokens
    TOKEN_VISIBLE_CHARS: Final[int] = 4

    # Mask character for hidden token portions
    TOKEN_MASK: Final[str] = "****"


# =============================================================================
# Singleton Instances
# =============================================================================

network = NetworkConfig()
retry = RetryConfig()
security = SecurityConfig()


# =============================================================================
# Utility Functions
# =============================================================================

def mask_token(token: str | None) -> str:
    """
    Mask a token for safe logging.

    Args:
        token: The token to mask. Can be None.

    Returns:
        A masked representation of the token:
        - "not set" if token is None or empty
        - "****XXXX" where XXXX is the last N visible characters
        - "set" if token is too short to show partial characters

    Examples:
        >>> mask_token(None)
        'not set'
        >>> mask_token("abc123def456")
        '****f456'
        >>> mask_token("abc")
        'set'
    """
    if not token:
        return "not set"

    if len(token) > security.TOKEN_VISIBLE_CHARS:
        return f"{security.TOKEN_MASK}{token[-security.TOKEN_VISIBLE_CHARS:]}"

    return "set"
