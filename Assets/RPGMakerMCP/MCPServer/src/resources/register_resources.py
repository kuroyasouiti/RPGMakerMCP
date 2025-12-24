"""
MCP Resources for RPGMaker Unite MCP Server
"""

from __future__ import annotations

from mcp import types as mcp_types
from mcp.server import Server


def register_resources(server: Server) -> None:
    """Register MCP resources.

    Resources provide read-only access to server state and information.
    Currently no resources are registered for RPGMaker Unite MCP.
    """

    @server.list_resources()
    async def list_resources() -> list[mcp_types.Resource]:
        """List all available resources."""
        return []

    @server.read_resource()
    async def read_resource(uri: str) -> str:
        """Read a resource by URI."""
        raise ValueError(f"Unknown resource URI: {uri}")
