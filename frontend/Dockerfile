# ---------- build: compile the React app to static files ----------
# Debian-based (glibc) image: Vite/TypeScript/oxlint ship native binaries for it.
FROM node:24-slim AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- final: nginx serves the static files ----------
FROM nginx:1.29-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

# Scripts in /docker-entrypoint.d run when the container starts (before nginx).
# This one writes config.js from the API_BASE_URL environment variable.
COPY --chmod=755 docker/40-app-config.sh /docker-entrypoint.d/40-app-config.sh

ENV API_BASE_URL=http://localhost:8080
EXPOSE 80
