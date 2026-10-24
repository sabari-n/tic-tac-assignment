# ---- deps: install exact versions from the lockfile ----
FROM node:24-alpine AS deps
WORKDIR /app
ENV NG_CLI_ANALYTICS=false
# Copy only the manifests first so `npm ci` is cached until dependencies change.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

# ---- test: run with `docker compose --profile test run --rm web-tests` ----
FROM deps AS test
COPY . .
CMD ["npm", "run", "test:ci"]

# ---- build: production bundle ----
FROM deps AS build
COPY . .
RUN npm run build

# ---- final: static files served by nginx, which also proxies /api ----
FROM nginx:stable-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80
