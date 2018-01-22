# Based on https://github.com/johnnyreilly/jQuery.Validation.Unobtrusive.Native/

param([string]$buildFolder, [string]$email, [string]$username, [string]$personalAccessToken)

if ($env:APPVEYOR_REPO_BRANCH -eq "master" -And $env:APPVEYOR_REPO_TAG -eq $true)
{
  Write-Host "- Set config settings...."
  git config --global user.email $email
  git config --global user.name $username
  git config --global push.default matching
  git config --global core.autocrlf true
  git config --global core.safecrlf false

  Write-Host "- Clone gh-pages branch...."
  cd "$($buildFolder)\..\"
  mkdir gh-pages
  git clone --quiet --branch=gh-pages https://$($username):$($personalAccessToken)@github.com/$($env:APPVEYOR_REPO_NAME).git .\gh-pages\
  cd gh-pages
  git status

  Write-Host "- Clean gh-pages folder...."
  Get-ChildItem -Attributes !r | Remove-Item -Recurse -Force

  Write-Host "- Copy contents of docs\_site folder into gh-pages folder...."
  Copy-Item -path "$($buildFolder)\docs\_site\*" -Destination $pwd.Path -Recurse

  git status
  $thereAreChanges = git status | select-string -pattern "Changes not staged for commit:","Untracked files:" -simplematch
  if ($thereAreChanges -ne $null)
  {
    Write-host "- Committing changes to documentation..."
    git add --all
    git status
    git commit -m "[skip ci] - static site regeneration"
    git status
    Write-Host "- Push it...."
    git push --quiet
  }
  else
  {
    Write-Host "- No changes to documentation to commit"
  }
}
else
{
  Write-Host "- Nothing to do. Doc is pushed only for tags on master"
}
