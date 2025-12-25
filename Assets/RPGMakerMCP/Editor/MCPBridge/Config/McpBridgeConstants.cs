using System;
using UnityEngine;

namespace MCP.Editor.Config
{
    /// <summary>
    /// Centralized configuration constants for MCP Bridge.
    /// Contains network, timeout, retry, and security settings.
    /// </summary>
    public static class McpBridgeConstants
    {
        #region Network Configuration

        /// <summary>
        /// Default path for WebSocket bridge endpoint.
        /// </summary>
        public const string BridgePath = "/bridge";

        /// <summary>
        /// Maximum size of HTTP handshake headers in bytes.
        /// </summary>
        public const int MaxHandshakeHeaderSize = 16 * 1024;

        /// <summary>
        /// Maximum size of WebSocket messages in bytes.
        /// </summary>
        public const int MaxMessageBytes = 2 * 1024 * 1024;

        /// <summary>
        /// Buffer size for reading HTTP requests.
        /// </summary>
        public const int HttpReadBufferSize = 4096;

        #endregion

        #region Retry Configuration

        /// <summary>
        /// Maximum number of retry attempts for sending messages.
        /// </summary>
        public const int MaxSendRetries = 3;

        /// <summary>
        /// Delay between send retries in milliseconds.
        /// </summary>
        public const int SendRetryDelayMs = 100;

        #endregion

        #region Timeout Configuration

        /// <summary>
        /// Interval between heartbeat pings.
        /// </summary>
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Maximum time to wait for heartbeat response before considering connection dead.
        /// </summary>
        public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Timeout for WebSocket send operations.
        /// </summary>
        public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Default timeout for bridge commands in milliseconds.
        /// </summary>
        public const int DefaultCommandTimeoutMs = 30000;

        #endregion

        #region EditorPrefs Keys

        /// <summary>
        /// Key for storing whether bridge was connected before compilation.
        /// </summary>
        public const string WasConnectedBeforeCompileKey = "McpBridge_WasConnectedBeforeCompile";

        /// <summary>
        /// Key for storing compilation start time.
        /// </summary>
        public const string CompilationStartTimeKey = "McpBridge_CompilationStartTime";

        /// <summary>
        /// Key for storing pending compilation result.
        /// </summary>
        public const string PendingCompilationResultKey = "McpBridge_PendingCompilationResult";

        #endregion

        #region Path Validation

        /// <summary>
        /// Validates that a path is within the Unity project directory.
        /// Prevents path traversal attacks.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="allowedRootPath">Optional custom root path. Defaults to Application.dataPath.</param>
        /// <returns>True if the path is safe, false otherwise.</returns>
        public static bool IsPathWithinProject(string path, string allowedRootPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            try
            {
                var rootPath = allowedRootPath ?? Application.dataPath;
                var fullPath = System.IO.Path.GetFullPath(path);
                var normalizedRoot = System.IO.Path.GetFullPath(rootPath);

                // Check if the path is within the allowed root
                return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // Path contains invalid characters or other issues
                return false;
            }
        }

        /// <summary>
        /// Validates and normalizes a file path for safe use.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="allowedRootPath">Optional custom root path.</param>
        /// <returns>The normalized path if valid.</returns>
        /// <exception cref="InvalidOperationException">Thrown if path is invalid or outside project.</exception>
        public static string ValidateAndNormalizePath(string path, string allowedRootPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Path cannot be null or empty.");
            }

            var rootPath = allowedRootPath ?? Application.dataPath;
            string fullPath;

            try
            {
                fullPath = System.IO.Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid path format: {ex.Message}");
            }

            var normalizedRoot = System.IO.Path.GetFullPath(rootPath);

            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Path traversal detected. Path must be within '{normalizedRoot}'.");
            }

            return fullPath;
        }

        #endregion

        #region Security

        /// <summary>
        /// Number of characters to show at the end of masked tokens.
        /// </summary>
        public const int TokenVisibleChars = 4;

        /// <summary>
        /// Mask a token for safe logging.
        /// </summary>
        /// <param name="token">The token to mask.</param>
        /// <returns>A masked representation of the token.</returns>
        public static string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "not set";
            }

            if (token.Length > TokenVisibleChars)
            {
                return $"****{token.Substring(token.Length - TokenVisibleChars)}";
            }

            return "set";
        }

        #endregion
    }
}
