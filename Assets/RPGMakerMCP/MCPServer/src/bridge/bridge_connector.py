from __future__ import annotations

import asyncio
from contextlib import suppress

import websockets
from websockets.asyncio.client import ClientConnection
from websockets.protocol import State as ConnectionState

from bridge.bridge_manager import bridge_manager
from config.constants import network, retry
from config.env import env
from logger import logger


class BridgeConnector:
    def __init__(self) -> None:
        self._task: asyncio.Task[None] | None = None
        self._stop_event = asyncio.Event()
        self._intentional_close = False

    def start(self) -> None:
        if self._task:
            return
        self._intentional_close = False
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run())

    async def stop(self) -> None:
        self._intentional_close = True
        self._stop_event.set()
        if not self._task:
            return

        self._task.cancel()
        with suppress(asyncio.CancelledError):
            await self._task
        self._task = None

    async def _run(self) -> None:
        delay_seconds = 0.0
        attempt_count = 0
        try:
            while not self._stop_event.is_set():
                if delay_seconds > 0:
                    logger.debug("Waiting %.2fs before reconnecting Unity bridge (attempt %d)", delay_seconds, attempt_count + 1)
                    try:
                        await asyncio.wait_for(self._stop_event.wait(), delay_seconds)
                        break
                    except asyncio.TimeoutError:
                        pass

                attempt_count += 1
                try:
                    await self._connect_once()
                    # Connection successful - reset attempt count and use configured delay
                    attempt_count = 0
                    delay_seconds = env.bridge_reconnect_ms / 1000
                except (ConnectionRefusedError, asyncio.TimeoutError) as exc:
                    # Expected connection errors - use appropriate retry strategy
                    if attempt_count <= retry.QUICK_RETRY_ATTEMPTS:
                        delay_seconds = retry.BACKOFF_BASE_DELAY * (2 ** (attempt_count - 1))
                        logger.info("Unity bridge connection attempt %d failed: %s (retrying in %.1fs)", attempt_count, exc, delay_seconds)
                    else:
                        delay_seconds = max(retry.MIN_RETRY_DELAY, env.bridge_reconnect_ms / 1000)
                        logger.warning("Unity bridge connection attempt %d failed: %s (retrying in %.1fs)", attempt_count, exc, delay_seconds)
                except OSError as exc:
                    # Network-related OS errors (e.g., network unreachable)
                    delay_seconds = max(retry.MIN_RETRY_DELAY, env.bridge_reconnect_ms / 1000)
                    logger.warning("Unity bridge network error on attempt %d: %s (retrying in %.1fs)", attempt_count, exc, delay_seconds)
                except Exception as exc:  # pragma: no cover - defensive
                    # Unexpected errors - log with full details
                    delay_seconds = max(retry.MIN_RETRY_DELAY, env.bridge_reconnect_ms / 1000)
                    logger.exception("Unexpected error on Unity bridge connection attempt %d: %s", attempt_count, exc)
        finally:
            self._task = None

    async def _connect_once(self) -> None:
        url = _build_ws_url(env.unity_bridge_host, env.unity_bridge_port, "/bridge")
        logger.info("Attempting connection to Unity bridge at %s", url)

        # Build authentication headers
        extra_headers: dict[str, str] = {}
        if env.bridge_token:
            extra_headers["Authorization"] = f"Bearer {env.bridge_token}"
            logger.debug("Using authentication token for bridge connection")

        try:
            # Connect with compatible settings for Unity's custom WebSocket implementation
            async with websockets.connect(
                url,
                open_timeout=network.WEBSOCKET_OPEN_TIMEOUT,
                close_timeout=network.WEBSOCKET_CLOSE_TIMEOUT,
                max_size=network.MAX_MESSAGE_SIZE,
                compression=None,  # Disable compression for compatibility
                ping_interval=None,  # Disable automatic ping (we handle it manually)
                ping_timeout=None,
                additional_headers=extra_headers if extra_headers else None,
            ) as socket:
                logger.debug("WebSocket connection established, waiting for authentication...")
                # Attach with auth headers
                await bridge_manager.attach(socket)
                logger.info("✅ Connected to Unity bridge successfully (session: %s)", bridge_manager.get_session_id())
                await self._monitor_connection(socket)
                logger.info("Unity bridge connection closed")
        except asyncio.TimeoutError:
            logger.warning("❌ Unity bridge connection timeout - is Unity Editor running with MCP Assistant started?")
            raise
        except ConnectionRefusedError:
            logger.warning("❌ Unity bridge connection refused - is Unity Editor running with MCP Assistant started?")
            raise
        except Exception as exc:
            logger.warning("❌ Unity bridge connection error: %s", exc)
            raise

    async def _monitor_connection(self, socket: ClientConnection) -> None:
        ping_interval = max(5.0, env.bridge_reconnect_ms / 1000)

        async def ping_loop() -> None:
            consecutive_failures = 0

            while not self._stop_event.is_set():
                await asyncio.sleep(ping_interval)
                try:
                    await bridge_manager.send_ping()
                    consecutive_failures = 0  # Reset on success
                except asyncio.TimeoutError:
                    consecutive_failures += 1
                    logger.warning("Unity bridge ping timeout (attempt %d/%d)", consecutive_failures, network.MAX_PING_FAILURES)
                    if consecutive_failures >= network.MAX_PING_FAILURES:
                        logger.error("Unity bridge ping timed out %d times consecutively - closing connection", network.MAX_PING_FAILURES)
                        return
                except ConnectionError as exc:
                    consecutive_failures += 1
                    logger.warning("Unity bridge ping connection error (attempt %d/%d): %s", consecutive_failures, network.MAX_PING_FAILURES, exc)
                    if consecutive_failures >= network.MAX_PING_FAILURES:
                        logger.error("Unity bridge ping failed %d times consecutively - closing connection", network.MAX_PING_FAILURES)
                        return
                except Exception as exc:  # pragma: no cover - defensive
                    consecutive_failures += 1
                    logger.warning("Unity bridge ping failed (attempt %d/%d): %s", consecutive_failures, network.MAX_PING_FAILURES, exc)
                    if consecutive_failures >= network.MAX_PING_FAILURES:
                        logger.error("Unity bridge ping failed %d times consecutively - closing connection", network.MAX_PING_FAILURES)
                        return

        ping_task = asyncio.create_task(ping_loop())
        stop_task = asyncio.create_task(self._stop_event.wait())
        wait_task = asyncio.create_task(socket.wait_closed())

        done, pending = await asyncio.wait(
            {ping_task, stop_task, wait_task},
            return_when=asyncio.FIRST_COMPLETED,
        )

        for task in pending:
            task.cancel()

        if stop_task in done and _is_socket_open(socket):
            with suppress(Exception):
                await socket.close(code=1000, reason="shutdown")

        for task in pending:
            with suppress(asyncio.CancelledError):
                await task

        if ping_task in done and ping_task.exception():
            logger.debug("Unity bridge ping task finished with: %s", ping_task.exception())

        if wait_task in done and wait_task.exception():
            logger.debug("Unity bridge wait_closed exception: %s", wait_task.exception())

        if stop_task in done:
            logger.info("Unity bridge connector stopping on request")

        if (
            wait_task in done
            and not self._intentional_close
            and not self._stop_event.is_set()
        ):
            logger.warning("Unity bridge connection closed unexpectedly")


bridge_connector = BridgeConnector()


def _is_socket_open(socket: ClientConnection) -> bool:
    return socket.state is not ConnectionState.CLOSED


def _build_ws_url(host: str, port: int, path: str) -> str:
    trimmed_host = (host or "").strip()
    if not trimmed_host:
        trimmed_host = "127.0.0.1"
    if ":" in trimmed_host and not (trimmed_host.startswith("[") and trimmed_host.endswith("]")):
        trimmed_host = f"[{trimmed_host}]"
    normalized_path = path if path.startswith("/") else f"/{path}"
    return f"ws://{trimmed_host}:{port}{normalized_path}"
