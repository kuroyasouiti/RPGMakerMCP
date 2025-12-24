"""
Tool registration for RPGMaker Unite MCP Server.
Registers RPGMaker-specific tools (8 tools) plus ping and compilation_await.
"""

from __future__ import annotations

from typing import Any

import mcp.types as types
from mcp.server import Server

from bridge.bridge_manager import bridge_manager
from logger import logger
from tools.rpgmaker_tools import RPGMAKER_TOOL_DEFINITIONS, RPGMAKER_TOOL_MAP
from utils.json_utils import as_pretty_json


def _ensure_bridge_connected() -> None:
    if not bridge_manager.is_connected():
        raise RuntimeError(
            "Unity bridge is not connected. In the Unity Editor choose Tools/MCP Assistant to start the bridge."
        )


async def _call_bridge_tool(tool_name: str, payload: dict[str, Any]) -> list[types.Content]:
    _ensure_bridge_connected()

    timeout_ms = 45_000
    if "timeoutSeconds" in payload:
        unity_timeout = payload["timeoutSeconds"]
        timeout_ms = (unity_timeout + 20) * 1000

    try:
        response = await bridge_manager.send_command(tool_name, payload, timeout_ms=timeout_ms)
    except Exception as exc:
        raise RuntimeError(f'Unity bridge tool "{tool_name}" failed: {exc}') from exc

    text = response if isinstance(response, str) else as_pretty_json(response)
    return [types.TextContent(type="text", text=text)]


def register_tools(server: Server) -> None:
    """Register all MCP tools for RPGMaker Unite."""

    # ============================================================
    # Ping Tool Schema
    # ============================================================
    ping_schema: dict[str, Any] = {
        "type": "object",
        "properties": {},
        "additionalProperties": False,
    }

    # ============================================================
    # Compilation Await Tool Schema
    # ============================================================
    compilation_await_schema: dict[str, Any] = {
        "type": "object",
        "properties": {
            "operation": {
                "type": "string",
                "enum": ["await"],
                "description": "Operation to perform. Currently only 'await' is supported.",
            },
            "timeoutSeconds": {
                "type": "integer",
                "description": "Maximum time to wait for compilation to complete (default: 60 seconds).",
                "default": 60,
            },
        },
        "required": ["operation"],
        "additionalProperties": False,
    }

    # ============================================================
    # Tool Definitions List
    # ============================================================
    tool_definitions: list[types.Tool] = [
        # Utility Tools
        types.Tool(
            name="unity_ping",
            description="Test connection to Unity bridge. Returns bridge status and session info.",
            inputSchema=ping_schema,
        ),
        types.Tool(
            name="unity_compilation_await",
            description=(
                "Wait for Unity script compilation to complete. "
                "Use after creating/updating C# scripts to ensure compilation finishes before proceeding."
            ),
            inputSchema=compilation_await_schema,
        ),
        # RPGMaker Tools (8 tools)
        *RPGMAKER_TOOL_DEFINITIONS,
    ]

    # ============================================================
    # Tool Name to Unity Bridge Tool Name Mapping
    # ============================================================
    tool_name_map: dict[str, str] = {
        "unity_ping": "ping",
        "unity_compilation_await": "compilationAwait",
        **RPGMAKER_TOOL_MAP,
    }

    # ============================================================
    # Register list_tools Handler
    # ============================================================
    @server.list_tools()
    async def list_tools() -> list[types.Tool]:
        return tool_definitions

    # ============================================================
    # Register call_tool Handler
    # ============================================================
    @server.call_tool()
    async def call_tool(
        name: str,
        arguments: dict[str, Any] | None,
    ) -> list[types.Content]:
        payload = arguments or {}
        logger.info("Tool call: %s", name)

        # Map tool name to Unity bridge tool name
        bridge_tool_name = tool_name_map.get(name)
        if bridge_tool_name is None:
            raise ValueError(f"Unknown tool: {name}")

        # Special handling for compilation_await
        if name == "unity_compilation_await":
            _ensure_bridge_connected()
            timeout_seconds = payload.get("timeoutSeconds", 60)
            try:
                result = await bridge_manager.await_compilation(timeout_seconds)
                return [types.TextContent(type="text", text=as_pretty_json(result))]
            except TimeoutError as exc:
                return [types.TextContent(type="text", text=as_pretty_json({
                    "success": False,
                    "timedOut": True,
                    "error": str(exc),
                }))]

        # Call Unity bridge for all other tools
        return await _call_bridge_tool(bridge_tool_name, payload)
