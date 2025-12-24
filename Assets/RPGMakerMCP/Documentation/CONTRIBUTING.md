# Contributing to Unity-AI-Forge

<div align="center">

**🤝 Welcome Contributors!**

Unity-AI-Forge へのコントリビューションをありがとうございます

[📚 Documentation Index](INDEX.md) | [🚀 Getting Started](GETTING_STARTED.md)

</div>

---

## 📋 Table of Contents

1. [コントリビューション方法](#-how-to-contribute)
2. [開発環境のセットアップ](#-development-setup)
3. [コーディング規約](#-coding-standards)
4. [コミットメッセージ](#-commit-messages)
5. [Pull Request](#-pull-requests)
6. [ドキュメント](#-documentation)
7. [テスト](#-testing)

---

## 🎯 How to Contribute

### コントリビューションの種類

| 種類 | 説明 | 難易度 |
|:---|:---|:---:|
| **🐛 Bug Fix** | バグを修正 | ⭐ |
| **📝 Documentation** | ドキュメント改善 | ⭐ |
| **✨ New Feature** | 新機能追加 | ⭐⭐⭐ |
| **🎨 Examples** | サンプル追加 | ⭐⭐ |
| **🧪 Tests** | テスト追加 | ⭐⭐ |
| **⚡ Performance** | パフォーマンス改善 | ⭐⭐⭐ |

---

## 🛠️ Development Setup

### 1. Repository を Fork

```bash
# 1. GitHub で Fork ボタンをクリック

# 2. Clone your fork
git clone https://github.com/YOUR_USERNAME/Unity-AI-Forge.git
cd Unity-AI-Forge

# 3. Upstream を追加
git remote add upstream https://github.com/kuroyasouiti/Unity-AI-Forge.git
```

### 2. Unity プロジェクトを開く

```bash
# Unity Hub で開く
# Unity Hub > Add > Select 'Unity-AI-Forge' folder
```

**Requirements:**
- Unity 2022.3 LTS or later
- Python 3.11+ (MCP Server開発の場合)
- .NET Standard 2.1

### 3. Branch を作成

```bash
# Feature branch を作成
git checkout -b feature/your-feature-name

# または Bug fix branch
git checkout -b fix/bug-description
```

---

## 📐 Coding Standards

### C# コーディング規約

Unity-AI-Forge は Microsoft の C# コーディング規約に従います。

#### Naming Conventions

```csharp
// ✅ Good
public class GameKitManager : MonoBehaviour  // PascalCase for classes
{
    public string ManagerId;                 // PascalCase for public fields
    private float currentValue;              // camelCase for private fields
    
    public void Initialize(string id)        // PascalCase for methods
    {
        var localVariable = 0;               // camelCase for local variables
    }
}

// ❌ Bad
public class gamekitmanager                  // Wrong case
{
    public string manager_id;                // Snake_case not allowed
    private float CurrentValue;              // Wrong case
}
```

#### Code Organization

```csharp
using UnityEngine;                           // Unity usings first
using System;                                // System usings second
using UnityAIForge.GameKit;                  // Project usings last

namespace UnityAIForge.GameKit               // Namespace matches folder structure
{
    /// <summary>
    /// XML documentation for public APIs
    /// </summary>
    public class YourClass : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private string id;
        #endregion
        
        #region Public Properties
        public string Id => id;
        #endregion
        
        #region Unity Lifecycle
        void Awake() { }
        void Start() { }
        #endregion
        
        #region Public Methods
        public void DoSomething() { }
        #endregion
        
        #region Private Methods
        private void HelperMethod() { }
        #endregion
    }
}
```

#### Comments

```csharp
// ✅ Good - Explain why, not what
// Use Singleton pattern to ensure only one instance
public static GameKitManager Instance { get; private set; }

// ❌ Bad - States the obvious
// Set the instance
public static GameKitManager Instance { get; private set; }
```

### Python コーディング規約

MCP Server は PEP 8 に従います。

```python
# ✅ Good
def process_game_object(game_object_path: str) -> dict:
    """Process a GameObject and return its data."""
    result = {
        "path": game_object_path,
        "success": True
    }
    return result

# ❌ Bad
def ProcessGameObject(GameObjectPath):
    result = {"path":GameObjectPath,"success":True}
    return result
```

---

## 💬 Commit Messages

### Conventional Commits

Unity-AI-Forge は [Conventional Commits](https://www.conventionalcommits.org/) を使用します。

#### Format

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

#### Types

| Type | 説明 | Example |
|:---|:---|:---|
| `feat` | 新機能 | `feat(gamekit): add save/load system` |
| `fix` | バグ修正 | `fix(actor): resolve movement bug` |
| `docs` | ドキュメント | `docs(readme): update installation steps` |
| `style` | コードスタイル | `style(manager): format code` |
| `refactor` | リファクタリング | `refactor(resource): simplify logic` |
| `test` | テスト追加 | `test(actor): add unit tests` |
| `chore` | ビルド/ツール | `chore(deps): update dependencies` |

#### Examples

```bash
# ✅ Good
feat(gamekit): add Machinations diagram support
fix(sceneflow): resolve transition bug on scene load
docs(getting-started): add Hello World example
test(resource-manager): add save/load tests

# ❌ Bad
Added new feature
Fixed bug
Update docs
Changes
```

#### Scope Guidelines

- `gamekit` - GameKit framework
- `mcp` - MCP Server
- `actor` - GameKitActor
- `manager` - GameKitManager
- `resource` - GameKitResourceManager
- `docs` - Documentation
- `tests` - Tests

---

## 🔀 Pull Requests

### PR の作成

1. **最新の main を pull**
   ```bash
   git checkout main
   git pull upstream main
   ```

2. **Feature branch にマージ**
   ```bash
   git checkout feature/your-feature
   git rebase main
   ```

3. **Push して PR 作成**
   ```bash
   git push origin feature/your-feature
   # GitHub で PR を作成
   ```

### PR Template

```markdown
## 📝 Description
Brief description of your changes

## 🎯 Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Performance improvement

## ✅ Checklist
- [ ] Code follows project coding standards
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Commit messages follow Conventional Commits
- [ ] No console errors/warnings

## 🧪 Testing
How did you test your changes?

## 📸 Screenshots (if applicable)
Add screenshots or GIFs

## 🔗 Related Issues
Closes #123
```

### PR Review Process

1. **Automated Checks**
   - Tests must pass
   - No linter errors
   - Code coverage maintained

2. **Code Review**
   - At least 1 approving review required
   - Address all comments

3. **Merge**
   - Squash and merge preferred
   - Rebase if needed

---

## 📚 Documentation

### ドキュメントの種類

| 種類 | 場所 | 目的 |
|:---|:---|:---|
| **API Docs** | Code comments | API reference |
| **Guides** | `Documentation/` | How-to guides |
| **Examples** | `Documentation/Examples/` | Tutorials |
| **README** | Each component | Quick overview |

### ドキュメントの書き方

#### Markdown Style

```markdown
# Title (H1) - One per document

## Major Section (H2)

### Subsection (H3)

#### Detail (H4)

- Use bullet points for lists
- Keep lines under 100 characters
- Use code blocks with language tags

```csharp
// Code examples should be complete and runnable
public class Example : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Hello World");
    }
}
```
```

#### Navigation Links

すべてのドキュメントに navigation を追加：

```markdown
[📚 Back to Index](../INDEX.md) | [🚀 Getting Started](GETTING_STARTED.md)
```

---

## 🧪 Testing

### テストの種類

#### 1. Unit Tests

```csharp
using NUnit.Framework;
using UnityAIForge.GameKit;

public class GameKitManagerTests
{
    [Test]
    public void Initialize_WithValidId_SetsManagerId()
    {
        var manager = CreateTestManager();
        manager.Initialize("TestManager");
        
        Assert.AreEqual("TestManager", manager.ManagerId);
    }
}
```

#### 2. Integration Tests

```csharp
[UnityTest]
public IEnumerator ResourceManager_ProcessFlows_UpdatesResources()
{
    var manager = CreateResourceManager();
    manager.AddResource("health", 100);
    
    yield return new WaitForSeconds(1f);
    
    Assert.Greater(manager.GetResourceValue("health"), 100);
}
```

### テストの実行

```bash
# Unity Test Runner で実行
# Window > General > Test Runner

# または CLI で
unity-editor -runTests -testPlatform EditMode
```

---

## 🎨 Design Guidelines

### GameKit Component の作成

新しい GameKit component を追加する場合：

1. **Namespace**: `UnityAIForge.GameKit`
2. **AddComponentMenu**: `[AddComponentMenu("UnityAIForge/GameKit/YourComponent")]`
3. **Documentation**: XML comments 必須
4. **Events**: UnityEvent を使用
5. **Inspector**: CustomEditor を提供

#### Template

```csharp
using UnityEngine;
using UnityEngine.Events;

namespace UnityAIForge.GameKit
{
    /// <summary>
    /// Brief description of your component.
    /// </summary>
    [AddComponentMenu("UnityAIForge/GameKit/YourComponent")]
    public class YourComponent : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] 
        [Tooltip("Description of this field")]
        private string yourField;
        #endregion
        
        #region Events
        public UnityEvent<string> OnYourEvent;
        #endregion
        
        #region Public API
        /// <summary>
        /// Initialize the component.
        /// </summary>
        public void Initialize(string parameter)
        {
            yourField = parameter;
            OnYourEvent?.Invoke(parameter);
        }
        #endregion
    }
}
```

---

## 🐛 Bug Reports

### Bug Report Template

Issues を作成する際：

```markdown
**Describe the bug**
A clear description of the bug

**To Reproduce**
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen

**Screenshots**
If applicable, add screenshots

**Environment:**
- Unity Version: [e.g. 2022.3.10f1]
- Unity-AI-Forge Version: [e.g. 2.1.0]
- OS: [e.g. Windows 11]

**Additional context**
Any other context about the problem
```

---

## 💡 Feature Requests

### Feature Request Template

```markdown
**Is your feature request related to a problem?**
A clear description of the problem

**Describe the solution you'd like**
What you want to happen

**Describe alternatives you've considered**
Alternative solutions

**Additional context**
Any other context or screenshots
```

---

## 🏆 Recognition

### Contributors

All contributors are recognized in:
- [CHANGELOG.md](CHANGELOG.md)
- GitHub Contributors page
- Release notes

### Types of Contributions

- 💻 Code
- 📖 Documentation
- 🐛 Bug reports
- 💡 Ideas
- 🧪 Testing
- 🌍 Translation
- 🎨 Design

---

## 📞 Questions?

- **General Questions**: [GitHub Discussions](https://github.com/kuroyasouiti/Unity-AI-Forge/discussions)
- **Bug Reports**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)
- **Feature Requests**: [GitHub Issues](https://github.com/kuroyasouiti/Unity-AI-Forge/issues)

---

<div align="center">

**Thank you for contributing! 🎉**

[📚 Documentation Index](INDEX.md) | [🚀 Getting Started](GETTING_STARTED.md)

</div>

