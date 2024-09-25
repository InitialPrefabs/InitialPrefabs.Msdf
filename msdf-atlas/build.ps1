param(
    [Parameter(Mandatory)][bool]$release
)

$src = "";
$dst = "..\Assets\com.initialprefabs.msdfgen\Plugins";
if ($release) {
    cargo build --release
    $src = "target/release/msdf_atlas.dll"
} else {
    cargo build
}

Copy-Item -Path $src -Destination $target