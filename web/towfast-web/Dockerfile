FROM node:24-alpine AS build
WORKDIR /app
RUN corepack enable
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY . .
RUN pnpm run build

FROM nginx:1.29-alpine
ENV API_BASE_URL="" \
    GOOGLE_MAPS_API_KEY="" \
    GOOGLE_MAPS_MAP_ID="" \
    TRACKING_INTERVAL_MS="10000"
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/towfast-web/browser /usr/share/nginx/html
COPY docker/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh
EXPOSE 80
