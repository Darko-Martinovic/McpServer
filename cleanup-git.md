# Git Cleanup Commands for Build Artifacts

## If you have already-tracked build files that you want to remove from Git:

### 1. Remove specific file types from Git tracking (but keep local files):

```bash
# Remove all DLL files from Git tracking
git rm --cached **/*.dll

# Remove all PDB files from Git tracking
git rm --cached **/*.pdb

# Remove all EXE files from Git tracking
git rm --cached **/*.exe

# Remove entire bin and obj directories from Git tracking
git rm -r --cached Database/bin Database/obj
git rm -r --cached **/bin **/obj
```

### 2. Remove all build artifacts in one command:

```bash
git rm -r --cached Database/bin Database/obj **/bin **/obj **/*.dll **/*.pdb **/*.exe
```

### 3. After removing from Git, commit the changes:

```bash
git add .gitignore
git commit -m "Remove build artifacts from Git tracking and update .gitignore"
```

### 4. If files keep appearing, force remove them:

```bash
# List all tracked files matching patterns
git ls-files | grep -E "\.(dll|pdb|exe)$|/bin/|/obj/"

# Remove them all
git ls-files | grep -E "\.(dll|pdb|exe)$|/bin/|/obj/" | xargs git rm --cached
```

## Note:

- `--cached` flag removes files from Git tracking but keeps them on your local disk
- After this, your .gitignore will prevent them from being tracked again
- You'll need to commit these changes to make them permanent
