using System.Collections.Generic;
using System.Text;

namespace MCP.Editor
{
    /// <summary>
    /// Provides code generation templates for common Unity design patterns.
    /// Supports Singleton, ObjectPool, StateMachine, Observer, Command, Factory, and ServiceLocator patterns.
    /// </summary>
    internal static class PatternTemplates
    {
        /// <summary>
        /// Generates C# code for a specified design pattern.
        /// </summary>
        /// <param name="patternType">Type of pattern to generate (singleton, objectpool, statemachine, observer, command, factory, servicelocator).</param>
        /// <param name="className">Name of the class to generate.</param>
        /// <param name="namespace">Optional namespace for the class.</param>
        /// <param name="options">Additional pattern-specific options.</param>
        /// <returns>Generated C# code as a string.</returns>
        public static string GeneratePattern(string patternType, string className, string @namespace = null, Dictionary<string, object> options = null)
        {
            options = options ?? new Dictionary<string, object>();

            return patternType.ToLower() switch
            {
                "singleton" => GenerateSingleton(className, @namespace, options),
                "objectpool" => GenerateObjectPool(className, @namespace, options),
                "statemachine" => GenerateStateMachine(className, @namespace, options),
                "observer" => GenerateObserver(className, @namespace, options),
                "command" => GenerateCommand(className, @namespace, options),
                "factory" => GenerateFactory(className, @namespace, options),
                "servicelocator" => GenerateServiceLocator(className, @namespace, options),
                _ => throw new System.ArgumentException($"Unknown pattern type: {patternType}"),
            };
        }

        private static string WrapInNamespace(string code, string @namespace)
        {
            if (string.IsNullOrEmpty(@namespace))
                return code;

            var sb = new StringBuilder();
            sb.AppendLine($"namespace {@namespace}");
            sb.AppendLine("{");
            foreach (var line in code.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine("    " + line);
                else
                    sb.AppendLine();
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static bool GetBool(Dictionary<string, object> options, string key, bool defaultValue = false)
        {
            if (options.TryGetValue(key, out var value))
            {
                if (value is bool b) return b;
                if (value is string s) return bool.TryParse(s, out var result) && result;
            }
            return defaultValue;
        }

        private static string GetString(Dictionary<string, object> options, string key, string defaultValue = "")
        {
            if (options.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        #region Singleton Pattern

        private static string GenerateSingleton(string className, string @namespace, Dictionary<string, object> options)
        {
            bool persistent = GetBool(options, "persistent", false);
            bool threadSafe = GetBool(options, "threadSafe", true);
            bool monoBehaviour = GetBool(options, "monoBehaviour", true);

            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            string baseClass = monoBehaviour ? "MonoBehaviour" : "";
            string classDeclaration = string.IsNullOrEmpty(baseClass)
                ? $"public class {className}"
                : $"public class {className} : {baseClass}";

            string code = $@"{classDeclaration}
{{
    private static {className} _instance;
    {(threadSafe ? "private static readonly object _lock = new object();" : "")}

    public static {className} Instance
    {{
        get
        {{
            {(threadSafe ? "lock (_lock)\n            {" : "")}
            if (_instance == null)
            {{
                {(monoBehaviour ? $@"_instance = FindObjectOfType<{className}>();
                if (_instance == null)
                {{
                    GameObject obj = new GameObject(""{className}"");
                    _instance = obj.AddComponent<{className}>();
                    {(persistent ? "DontDestroyOnLoad(obj);" : "")}
                }}" : $"_instance = new {className}();")}
            }}
            return _instance;
            {(threadSafe ? "}" : "")}
        }}
    }}

    {(monoBehaviour ? $@"private void Awake()
    {{
        if (_instance != null && _instance != this)
        {{
            Destroy(gameObject);
            return;
        }}
        _instance = this;
        {(persistent ? "DontDestroyOnLoad(gameObject);" : "")}
    }}" : "")}

    // Add your singleton methods and properties here
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion

        #region ObjectPool Pattern

        private static string GenerateObjectPool(string className, string @namespace, Dictionary<string, object> options)
        {
            string pooledType = GetString(options, "pooledType", "GameObject");
            int defaultCapacity = int.TryParse(GetString(options, "defaultCapacity", "10"), out var cap) ? cap : 10;
            int maxSize = int.TryParse(GetString(options, "maxSize", "100"), out var max) ? max : 100;

            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.Pool;");
            sb.AppendLine("using System;");
            sb.AppendLine();

            string code = $@"public class {className} : MonoBehaviour
{{
    [SerializeField] private {pooledType} prefab;
    [SerializeField] private int defaultCapacity = {defaultCapacity};
    [SerializeField] private int maxSize = {maxSize};

    private ObjectPool<{pooledType}> _pool;

    private void Awake()
    {{
        _pool = new ObjectPool<{pooledType}>(
            CreatePooledItem,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            true,
            defaultCapacity,
            maxSize
        );
    }}

    private {pooledType} CreatePooledItem()
    {{
        {pooledType} obj = Instantiate(prefab);
        return obj;
    }}

    private void OnTakeFromPool({pooledType} obj)
    {{
        {(pooledType == "GameObject" ? "obj.SetActive(true);" : "obj.gameObject.SetActive(true);")}
    }}

    private void OnReturnedToPool({pooledType} obj)
    {{
        {(pooledType == "GameObject" ? "obj.SetActive(false);" : "obj.gameObject.SetActive(false);")}
    }}

    private void OnDestroyPoolObject({pooledType} obj)
    {{
        {(pooledType == "GameObject" ? "Destroy(obj);" : "Destroy(obj.gameObject);")}
    }}

    public {pooledType} Get()
    {{
        return _pool.Get();
    }}

    public void Release({pooledType} obj)
    {{
        _pool.Release(obj);
    }}

    public void Clear()
    {{
        _pool.Clear();
    }}
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion

        #region StateMachine Pattern

        private static string GenerateStateMachine(string className, string @namespace, Dictionary<string, object> options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            string code = $@"// Base State Interface
public interface IState
{{
    void Enter();
    void Execute();
    void Exit();
}}

// State Machine
public class {className} : MonoBehaviour
{{
    private IState _currentState;
    private Dictionary<Type, IState> _states = new Dictionary<Type, IState>();

    public void RegisterState<T>(T state) where T : IState
    {{
        _states[typeof(T)] = state;
    }}

    public void ChangeState<T>() where T : IState
    {{
        if (_currentState != null)
        {{
            _currentState.Exit();
        }}

        if (_states.TryGetValue(typeof(T), out var newState))
        {{
            _currentState = newState;
            _currentState.Enter();
        }}
        else
        {{
            Debug.LogError($""State {{typeof(T).Name}} not registered!"");
        }}
    }}

    private void Update()
    {{
        _currentState?.Execute();
    }}
}}

// Example States
public class IdleState : IState
{{
    public void Enter() {{ Debug.Log(""Entering Idle State""); }}
    public void Execute() {{ /* Idle logic */ }}
    public void Exit() {{ Debug.Log(""Exiting Idle State""); }}
}}

public class MoveState : IState
{{
    public void Enter() {{ Debug.Log(""Entering Move State""); }}
    public void Execute() {{ /* Move logic */ }}
    public void Exit() {{ Debug.Log(""Exiting Move State""); }}
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion

        #region Observer Pattern

        private static string GenerateObserver(string className, string @namespace, Dictionary<string, object> options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            string code = $@"// Event System (Observer Pattern)
public class {className} : MonoBehaviour
{{
    private static {className} _instance;
    public static {className} Instance
    {{
        get
        {{
            if (_instance == null)
            {{
                GameObject obj = new GameObject(""{className}"");
                _instance = obj.AddComponent<{className}>();
                DontDestroyOnLoad(obj);
            }}
            return _instance;
        }}
    }}

    private Dictionary<string, Delegate> _eventTable = new Dictionary<string, Delegate>();

    public void Subscribe<T>(string eventName, Action<T> listener)
    {{
        if (!_eventTable.ContainsKey(eventName))
        {{
            _eventTable[eventName] = null;
        }}
        _eventTable[eventName] = (Action<T>)_eventTable[eventName] + listener;
    }}

    public void Unsubscribe<T>(string eventName, Action<T> listener)
    {{
        if (_eventTable.ContainsKey(eventName))
        {{
            _eventTable[eventName] = (Action<T>)_eventTable[eventName] - listener;
        }}
    }}

    public void Publish<T>(string eventName, T data)
    {{
        if (_eventTable.TryGetValue(eventName, out var del))
        {{
            (del as Action<T>)?.Invoke(data);
        }}
    }}

    // Parameterless events
    public void Subscribe(string eventName, Action listener)
    {{
        if (!_eventTable.ContainsKey(eventName))
        {{
            _eventTable[eventName] = null;
        }}
        _eventTable[eventName] = (Action)_eventTable[eventName] + listener;
    }}

    public void Unsubscribe(string eventName, Action listener)
    {{
        if (_eventTable.ContainsKey(eventName))
        {{
            _eventTable[eventName] = (Action)_eventTable[eventName] - listener;
        }}
    }}

    public void Publish(string eventName)
    {{
        if (_eventTable.TryGetValue(eventName, out var del))
        {{
            (del as Action)?.Invoke();
        }}
    }}
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion

        #region Command Pattern

        private static string GenerateCommand(string className, string @namespace, Dictionary<string, object> options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            string code = $@"// Command Interface
public interface ICommand
{{
    void Execute();
    void Undo();
}}

// Command Invoker
public class {className} : MonoBehaviour
{{
    private Stack<ICommand> _commandHistory = new Stack<ICommand>();
    private Stack<ICommand> _redoStack = new Stack<ICommand>();

    public void ExecuteCommand(ICommand command)
    {{
        command.Execute();
        _commandHistory.Push(command);
        _redoStack.Clear();
    }}

    public void Undo()
    {{
        if (_commandHistory.Count > 0)
        {{
            ICommand command = _commandHistory.Pop();
            command.Undo();
            _redoStack.Push(command);
        }}
        else
        {{
            Debug.Log(""No commands to undo"");
        }}
    }}

    public void Redo()
    {{
        if (_redoStack.Count > 0)
        {{
            ICommand command = _redoStack.Pop();
            command.Execute();
            _commandHistory.Push(command);
        }}
        else
        {{
            Debug.Log(""No commands to redo"");
        }}
    }}

    public void ClearHistory()
    {{
        _commandHistory.Clear();
        _redoStack.Clear();
    }}
}}

// Example Command: Move Object
public class MoveCommand : ICommand
{{
    private Transform _transform;
    private Vector3 _newPosition;
    private Vector3 _previousPosition;

    public MoveCommand(Transform transform, Vector3 newPosition)
    {{
        _transform = transform;
        _newPosition = newPosition;
    }}

    public void Execute()
    {{
        _previousPosition = _transform.position;
        _transform.position = _newPosition;
    }}

    public void Undo()
    {{
        _transform.position = _previousPosition;
    }}
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion

        #region Factory Pattern

        private static string GenerateFactory(string className, string @namespace, Dictionary<string, object> options)
        {
            string productType = GetString(options, "productType", "GameObject");

            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            string code = $@"// Product Base Class/Interface
public abstract class Product
{{
    public abstract void Initialize();
}}

// Factory Pattern
public class {className} : MonoBehaviour
{{
    [System.Serializable]
    public class ProductPrefab
    {{
        public string productId;
        public GameObject prefab;
    }}

    [SerializeField] private List<ProductPrefab> productPrefabs = new List<ProductPrefab>();
    private Dictionary<string, GameObject> _prefabDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {{
        foreach (var product in productPrefabs)
        {{
            if (!string.IsNullOrEmpty(product.productId) && product.prefab != null)
            {{
                _prefabDictionary[product.productId] = product.prefab;
            }}
        }}
    }}

    public GameObject CreateProduct(string productId)
    {{
        if (_prefabDictionary.TryGetValue(productId, out var prefab))
        {{
            GameObject instance = Instantiate(prefab);
            return instance;
        }}
        else
        {{
            Debug.LogError($""Product with ID '{{productId}}' not found!"");
            return null;
        }}
    }}

    public T CreateProduct<T>(string productId) where T : Component
    {{
        GameObject obj = CreateProduct(productId);
        if (obj != null)
        {{
            return obj.GetComponent<T>();
        }}
        return null;
    }}

    public GameObject CreateProduct(string productId, Vector3 position, Quaternion rotation)
    {{
        if (_prefabDictionary.TryGetValue(productId, out var prefab))
        {{
            GameObject instance = Instantiate(prefab, position, rotation);
            return instance;
        }}
        else
        {{
            Debug.LogError($""Product with ID '{{productId}}' not found!"");
            return null;
        }}
    }}
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion

        #region ServiceLocator Pattern

        private static string GenerateServiceLocator(string className, string @namespace, Dictionary<string, object> options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            string code = $@"// Service Locator Pattern
public class {className} : MonoBehaviour
{{
    private static {className} _instance;
    public static {className} Instance
    {{
        get
        {{
            if (_instance == null)
            {{
                GameObject obj = new GameObject(""{className}"");
                _instance = obj.AddComponent<{className}>();
                DontDestroyOnLoad(obj);
            }}
            return _instance;
        }}
    }}

    private Dictionary<Type, object> _services = new Dictionary<Type, object>();

    private void Awake()
    {{
        if (_instance != null && _instance != this)
        {{
            Destroy(gameObject);
            return;
        }}
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }}

    public void RegisterService<T>(T service) where T : class
    {{
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {{
            Debug.LogWarning($""Service of type {{type.Name}} is already registered. Overwriting..."");
        }}
        _services[type] = service;
    }}

    public void UnregisterService<T>() where T : class
    {{
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {{
            _services.Remove(type);
        }}
        else
        {{
            Debug.LogWarning($""Service of type {{type.Name}} is not registered."");
        }}
    }}

    public T GetService<T>() where T : class
    {{
        Type type = typeof(T);
        if (_services.TryGetValue(type, out var service))
        {{
            return service as T;
        }}
        else
        {{
            Debug.LogError($""Service of type {{type.Name}} not found!"");
            return null;
        }}
    }}

    public bool HasService<T>() where T : class
    {{
        return _services.ContainsKey(typeof(T));
    }}
}}

// Example Service Interface
public interface IAudioService
{{
    void PlaySound(string soundId);
    void StopSound(string soundId);
}}

// Example Service Implementation
public class AudioService : IAudioService
{{
    public void PlaySound(string soundId)
    {{
        Debug.Log($""Playing sound: {{soundId}}"");
        // Implementation here
    }}

    public void StopSound(string soundId)
    {{
        Debug.Log($""Stopping sound: {{soundId}}"");
        // Implementation here
    }}
}}";

            sb.Append(code);
            return WrapInNamespace(sb.ToString(), @namespace);
        }

        #endregion
    }
}
