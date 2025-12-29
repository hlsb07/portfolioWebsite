#!/usr/bin/env bash
set -euo pipefail

### Configuration
REPO_URL="git@github.com:hlsb07/portfolioWebsite.git"

# Where the repo lives on the server
CODE_BASE="${CODE_BASE:-$HOME/code}"
REPO_DIR="$CODE_BASE/JobApplication"

# Frontend lives in frontend/public (static files)
FRONTEND_SRC="$REPO_DIR/frontend/public"
FRONTEND_TARGET="$HOME/docker/reverseProxy/www/portfolio"

# Backend stack (compose + API) should live in /docker/portfolio
BACKEND_SRC="$REPO_DIR/backend"
BACKEND_TARGET="$HOME/docker/portfolio"

echo "==> Ensure base directories exist…"
mkdir -p "$CODE_BASE" "$FRONTEND_TARGET" "$BACKEND_TARGET"

echo "==> Clone or update repository…"
if [ ! -d "$REPO_DIR/.git" ]; then
  git clone "$REPO_URL" "$REPO_DIR"
else
  git -C "$REPO_DIR" pull
fi

# Optional: add build steps here if you introduce a frontend build toolchain
# or need to publish the backend outside of Docker.

echo "==> Sync frontend to Nginx web root…"
rsync -av --delete \
  --exclude ".git" \
  --exclude ".gitignore" \
  "$FRONTEND_SRC"/ "$FRONTEND_TARGET"/

echo "==> Sync backend stack to $BACKEND_TARGET …"
rsync -av --delete \
  --exclude ".git" \
  --exclude ".gitignore" \
  --exclude ".env" \
  "$BACKEND_SRC"/ "$BACKEND_TARGET/backend/"

# Keep docker-compose next to the backend folder; keep .env untouched so secrets stay on the server
rsync -av \
  --exclude ".env" \
  "$REPO_DIR/docker-compose.yml" "$BACKEND_TARGET/"

echo "==> Done."
echo "Frontend deployed to: $FRONTEND_TARGET"
echo "Backend stack ready in: $BACKEND_TARGET (run docker-compose from here)"
