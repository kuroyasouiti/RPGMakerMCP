using System.Collections.Generic;

namespace MCP.Editor.Interfaces
{
    /// <summary>
    /// RectTransform操作を実行するハンドラーのインターフェース。
    /// UguiManageCommandHandlerの内部で機能別に分割されたサブハンドラーが実装します。
    /// </summary>
    public interface IRectTransformOperationHandler
    {
        /// <summary>
        /// このハンドラーがサポートする操作のリスト。
        /// </summary>
        IEnumerable<string> SupportedOperations { get; }
        
        /// <summary>
        /// 指定された操作を実行します。
        /// </summary>
        /// <param name="operation">操作名（例: "rectAdjust", "setAnchor"）</param>
        /// <param name="payload">操作パラメータ</param>
        /// <returns>操作結果</returns>
        object Execute(string operation, Dictionary<string, object> payload);
    }
}

