FROM rust:1.80-alpine AS builder

RUN apk add --no-cache musl-dev pkgconfig openssl-dev

WORKDIR /usr/src/app

COPY . .

RUN cargo build --release

FROM alpine:latest

RUN apk add --no-cache openssl ca-certificates

COPY --from=builder /usr/src/app/target/release/ruzenbot-rs /usr/local/bin/ruzenbot-rs

CMD ["ruzenbot-rs"]
