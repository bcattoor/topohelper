function CopyToEmptyFolder($source, $target ) {
    DeleteIfExistsAndCreateEmptyFolder($target )
    Copy-Item $source\* $target -recurse -force
}
function DeleteIfExistsAndCreateEmptyFolder($dir ) {
    if ( Test-Path $dir ) {
        #http://stackoverflow.com/questions/7909167/how-to-quietly-remove-a-directory-with-content-in-powershell/9012108#9012108
        Get-ChildItem -Path  $dir -Force -Recurse | Remove-Item -force -recurse
        Remove-Item $dir -Force

    }
    New-Item -ItemType Directory -Force -Path $dir
}

CopyToEmptyFolder ".\_build\html" ".\docs"