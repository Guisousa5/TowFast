#!/bin/sh
set -eu

envsubst < /usr/share/nginx/html/config.template.json > /usr/share/nginx/html/config.json
