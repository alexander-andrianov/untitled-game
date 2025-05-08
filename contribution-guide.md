# 🛠 Contribution Guide

Thank you for your interest in contributing! Please follow the guidelines below to help us maintain a clean and consistent codebase.

---

## 📁 Branches

- Use `develop` as the stable branch — **do not commit directly** to it.  
- Create a new branch for each feature or fix:
  ```bash
  git checkout -b feature/your-name/your-feature-name
  ```
- Branch naming convention:
  - `feature/your-name/xyz` — for new features  
  - `fix/your-name/xyz` — for bug fixes  
  - `chore/your-name/xyz` — for maintenance, tooling, etc.

---

## ✏️ Commits

- Write clear, concise commit messages in **English**.  
- Use [Conventional Commits](https://www.conventionalcommits.org/) (recommended):

  Examples:
  ```
  feat: added user login screen
  fix: corrected player movement on mobile
  chore: updated dependencies
  ```

---

## 🔁 Pull Requests

- Create a pull request from your branch into `develop`.  
- Title should clearly describe the change:  
  _e.g._ `Fixed enemy AI pathfinding`.

- Keep PRs focused and small. One PR = one logical change.

- Include a brief description of:
  - **What** you changed  
  - **Why** it was needed

- Link related issues (if any), using keywords like `Fixes #123`.

---

## ✅ Code Review

- All PRs will be reviewed before merging.  
- Please be open to feedback and make necessary changes.
