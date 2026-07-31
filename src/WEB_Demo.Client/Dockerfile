FROM node:24-alpine AS build
WORKDIR /app

COPY src/WEB_Demo.Client/package.json src/WEB_Demo.Client/package-lock.json ./
RUN npm ci

COPY src/WEB_Demo.Client/ ./
RUN npm run build

FROM nginx:1.29-alpine AS final
COPY deploy/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 443
