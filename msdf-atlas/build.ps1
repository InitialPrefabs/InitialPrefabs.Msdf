param(
    [Parameter(Mandatory)][bool]$release
)

$dllSource;
$dllTarget = "../Assets/com.initialprefabs.msdfgen/Plugins";

$csharpSource = "MsdfAtlas.cs";
$csharpTarget = "../Assets/com.initialprefabs.msdfgen/InitialPrefabs.Msdf.EditorExtensions/MsdfAtlas.cs"

if ($release) {
    cargo build --release
    $dllSource = "target/release/msdf_atlas.dll"
} else {
    cargo build
    $dllSource = "target/debug/msdf_atlas.dll"
}

Copy-Item -Path $dllSource -Destination $dllTarget
Copy-Item -Path $csharpSource -Destination $csharpTarget
