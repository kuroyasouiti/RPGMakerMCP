using MCP.Editor.Handlers;
using MCP.Editor.Handlers.RPGMaker;
using UnityEditor;
using UnityEngine;

namespace MCP.Editor.Base
{
    /// <summary>
    /// コマンドハンドラーの初期化クラス。
    /// Unity起動時またはコンパイル後に自動的にハンドラーを登録します。
    /// </summary>
    [InitializeOnLoad]
    public static class CommandHandlerInitializer
    {
        /// <summary>
        /// 静的コンストラクタ。Unity起動時に自動実行されます。
        /// </summary>
        static CommandHandlerInitializer()
        {
            // コンパイル完了後に初期化
            EditorApplication.delayCall += InitializeHandlers;
        }

        /// <summary>
        /// 全てのコマンドハンドラーを初期化して登録します。
        /// </summary>
        public static void InitializeHandlers()
        {
            try
            {
                Debug.Log("[CommandHandlerInitializer] Initializing command handlers...");

                // 既存のハンドラーをクリア（再初期化時）
                CommandHandlerFactory.Clear();

                // ユーティリティハンドラーを登録
                RegisterUtilityHandlers();

                // RPGMaker Uniteツールのハンドラーを登録
                RegisterRPGMakerHandlers();

                // 統計情報をログ出力
                var stats = CommandHandlerFactory.GetStatistics();
                Debug.Log($"[CommandHandlerInitializer] Initialized {stats["totalHandlers"]} command handlers");

                // 詳細ログ
                var handlers = (System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>)stats["registeredHandlers"];
                foreach (var handlerInfo in handlers)
                {
                    Debug.Log($"  - {handlerInfo["toolName"]}: {handlerInfo["category"]} (v{handlerInfo["version"]})");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CommandHandlerInitializer] Failed to initialize handlers: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// ユーティリティハンドラーを登録します。
        /// </summary>
        private static void RegisterUtilityHandlers()
        {
            CommandHandlerFactory.Register("ping", new PingHandler());
            CommandHandlerFactory.Register("compilationAwait", new CompilationAwaitHandler());
        }

        /// <summary>
        /// RPGMaker Uniteツールのハンドラーを登録します。
        /// </summary>
        private static void RegisterRPGMakerHandlers()
        {
            CommandHandlerFactory.Register("rpgMakerDatabase", new RPGMakerDatabaseHandler());
            CommandHandlerFactory.Register("rpgMakerMap", new RPGMakerMapHandler());
            CommandHandlerFactory.Register("rpgMakerEvent", new RPGMakerEventHandler());
            CommandHandlerFactory.Register("rpgMakerBattle", new RPGMakerBattleHandler());
            CommandHandlerFactory.Register("rpgMakerSystem", new RPGMakerSystemHandler());
            CommandHandlerFactory.Register("rpgMakerAssets", new RPGMakerAssetsHandler());
            CommandHandlerFactory.Register("rpgMakerGameState", new RPGMakerGameStateHandler());
            CommandHandlerFactory.Register("rpgMakerAudio", new RPGMakerAudioHandler());
        }
    }
}
