# syntax=docker/dockerfile:1.7
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json ./
RUN npm install --no-audit --no-fund

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    PORT=3000 \
    HOSTNAME=0.0.0.0
RUN addgroup -S getcode && adduser -S getcode -G getcode
COPY --from=build --chown=getcode:getcode /app/.next/standalone ./
COPY --from=build --chown=getcode:getcode /app/.next/static ./.next/static
COPY --from=build --chown=getcode:getcode /app/public ./public
USER getcode
EXPOSE 3000
CMD ["node", "server.js"]
