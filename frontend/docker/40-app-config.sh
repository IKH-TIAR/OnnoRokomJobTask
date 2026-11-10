#!/bin/sh
# Runs at container start (nginx image entrypoint hook).
# Writes the backend URL into config.js so it can be changed with an environment
# variable, without rebuilding the image. The URL is used by the user's browser,
# so it must be reachable from the browser (e.g. http://localhost:8080).
set -eu

API_BASE_URL="${API_BASE_URL:-http://localhost:8080}"

# Escape backslashes and single quotes for a JavaScript string literal.
escaped=$(printf '%s' "$API_BASE_URL" | sed -e 's/\\/\\\\/g' -e "s/'/\\\\'/g")

cat > /usr/share/nginx/html/config.js <<EOF
window.__APP_CONFIG__ = { apiBaseUrl: '${escaped}' }
EOF

echo "40-app-config.sh: frontend will call the backend at ${API_BASE_URL}"
