FROM rust:1.96.1-alpine3.24 AS builder

RUN apk add --no-cache musl-dev pkgconfig openssl-dev

WORKDIR /usr/src/app

COPY Cargo.lock Cargo.toml ./

RUN mkdir src && echo "fn main() {}" > src/main.rs

RUN cargo build

RUN rm -rf src/*.rs

COPY src ./src

RUN cargo build --release

FROM alpine:latest

RUN apk add --no-cache openssl ca-certificates

COPY --from=builder /usr/src/app/target/release/ruzenbot-rs /usr/local/bin/ruzenbot-rs

CMD ["ruzenbot-rs"]
