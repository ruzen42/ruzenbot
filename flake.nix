{
  description = "RuzenBot (Rust edition) - Telegram bot on teloxide + redb";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };

        rustToolchain = pkgs.rustc;
        cargoBin = pkgs.cargo;

        nativeBuildDeps = with pkgs; [
          clang
          llvmPackages.bintools 
          pkg-config
        ];

        buildDeps = with pkgs; [
          openssl
          openssl.dev
        ];

        ruzenbot-rs = pkgs.rustPlatform.buildRustPackage {
          pname = "ruzenbot-rs";
          version = "0.1.0";

          src = ./.;

          cargoLock = {
            lockFile = ./Cargo.lock;
          };

          nativeBuildInputs = nativeBuildDeps;
          buildInputs = buildDeps;

          OPENSSL_NO_VENDOR = 1;
          PKG_CONFIG_PATH = "${pkgs.openssl.dev}/lib/pkgconfig";

          CC = "${pkgs.clang}/bin/clang";
          RUSTFLAGS = "-C linker=${pkgs.clang}/bin/clang -C link-arg=-fuse-ld=lld";

          meta = with pkgs.lib; {
            description = "Telegram bot: RuzenBot, rewritten in Rust with teloxide + redb";
            license = licenses.mit;
            mainProgram = "ruzenbot-rs";
          };
        };
      in
      {
        packages.default = ruzenbot-rs;
        packages.ruzenbot-rs = ruzenbot-rs;

        apps.default = flake-utils.lib.mkApp {
          drv = ruzenbot-rs;
          name = "ruzenbot-rs";
        };

        devShells.default = pkgs.mkShell {
          nativeBuildInputs = nativeBuildDeps ++ [ rustToolchain cargoBin ];
          buildInputs = buildDeps;

          OPENSSL_NO_VENDOR = 1;
          PKG_CONFIG_PATH = "${pkgs.openssl.dev}/lib/pkgconfig";
          CC = "${pkgs.clang}/bin/clang";
          RUSTFLAGS = "-C linker=${pkgs.clang}/bin/clang -C link-arg=-fuse-ld=lld";

          shellHook = ''
            echo "set TOKEN, ADMIN_CHAT_ID, SHELL_RUNNER_URL, DB_PATH"
          '';
        };
      });
}
