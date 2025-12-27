{
  description = "Zako IssueTracker - Discord bot for issue tracking";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in
      {
        devShells.default = pkgs.mkShell {
          buildInputs = with pkgs; [
            dotnet-sdk_9
            sqlite
            git
          ];

          shellHook = ''
            export DOTNET_ROOT="${pkgs.dotnet-sdk_9}"
            export DOTNET_CLI_TELEMETRY_OPTOUT=1
            echo "🚀 Zako IssueTracker development environment"
            echo "📦 .NET SDK: $(dotnet --version)"
            echo "💾 SQLite: $(sqlite3 --version)"
            echo ""
            echo "💡 Set environment variables in .env file:"
            echo "   - DISCORD_TOKEN"
            echo "   - ADMIN_IDS"
            echo "   - SQLITE_FILE"
          '';
        };
      }
    );
}
