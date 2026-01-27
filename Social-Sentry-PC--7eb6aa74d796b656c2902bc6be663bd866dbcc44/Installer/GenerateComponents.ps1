$sourceDir = Resolve-Path "Installer/source"
$outputFile = "Installer/Components.wxs"
$script:componentRefs = @()

function Process-Directory {
    param (
        $currentPath
    )
    
    $localContent = ""
    
    # Files
    $files = Get-ChildItem -Path $currentPath -File
    foreach ($file in $files) {
        $id = "cmp_" + [Guid]::NewGuid().ToString("N")
        $fileId = "file_" + [Guid]::NewGuid().ToString("N")
        # Relative path logic:
        # We need relative path from the PROJECT file (Installer/) to the file (Installer/source/...)
        # Since source is inside Installer, we just need "source/..."
        # $file.FullName is E:\...\Installer\source\path\to\file
        # $sourceDir.Path is E:\...\Installer\source
        
        # Substring logic
        $rel = $file.FullName.Substring($sourceDir.Path.Length + 1)
        
        $localContent += @"
            <Component Id="$id" Guid="$([Guid]::NewGuid())" Bitness="always64">
                <File Id="$fileId" Source="source\$rel" KeyPath="yes" />
            </Component>
"@
        $script:componentRefs += $id
    }
    
    # Subdirectories
    $dirs = Get-ChildItem -Path $currentPath -Directory
    foreach ($dir in $dirs) {
        $dirId = "dir_" + [Guid]::NewGuid().ToString("N")
        $dirName = $dir.Name
        
        $localContent += @"
            <Directory Id="$dirId" Name="$dirName">
"@
        $localContent += Process-Directory -currentPath $dir.FullName
        $localContent += @"
            </Directory>
"@
    }
    
    return $localContent
}

$treeContent = Process-Directory -currentPath $sourceDir

$wixContent = @"
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <DirectoryRef Id="INSTALLFOLDER">
$treeContent
    </DirectoryRef>
  </Fragment>
  <Fragment>
    <ComponentGroup Id="HarvestedComponents">
"@

foreach ($ref in $script:componentRefs) {
    $wixContent += @"
      <ComponentRef Id="$ref" />
"@
}

$wixContent += @"
    </ComponentGroup>
  </Fragment>
</Wix>
"@

Set-Content -Path $outputFile -Value $wixContent
Write-Host "Generated $outputFile"
