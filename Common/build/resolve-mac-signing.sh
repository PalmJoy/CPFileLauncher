#!/bin/zsh
# 呼出元でROOTを設定してsourceする。証明書を明示し、暗黙の署名変更を防ぐ。
IDENTITY=${CODESIGN_IDENTITY:-}
if [[ -z "$IDENTITY" && -f "$ROOT/work/macos-signing.identity" ]]; then
  IDENTITY=$(<"$ROOT/work/macos-signing.identity")
  if [[ -z "$IDENTITY" ]]; then
    echo "work/macos-signing.identity が空です。署名IDを指定してください。" >&2
    exit 1
  fi
fi
IDENTITY=${IDENTITY:-FileLauncher Dev}
IDENTITY_NAME=$IDENTITY

if [[ "$IDENTITY" != '-' ]]; then
  identities=$(security find-identity -p codesigning -v)
  pattern='([[:xdigit:]]{40}) "([^"]+)"'
  found=0
  while IFS= read -r line; do
    if [[ "$line" =~ "$pattern" ]]; then
      hash=$match[1]
      name=$match[2]
      if [[ "${IDENTITY:u}" == "$hash" || "$IDENTITY" == "$name" ]]; then
        (( found += 1 ))
        IDENTITY_HASH=$hash
        IDENTITY_NAME=$name
      fi
    fi
  done <<< "$identities"
  if (( found != 1 )); then
    echo "署名証明書を一意に取得できません。CODESIGN_IDENTITY、または work/macos-signing.identity に有効なコード署名証明書を指定してください。ad-hocへは自動変更しません。" >&2
    exit 1
  fi
  IDENTITY=$IDENTITY_HASH
fi

SIGN_OPTIONS=(--timestamp=none)
if [[ "$IDENTITY_NAME" == "Developer ID Application:"* ]]; then
  SIGN_OPTIONS=(--timestamp --options runtime)
fi
if [[ -n "${NOTARYTOOL_PROFILE:-}" && "$IDENTITY_NAME" != "Developer ID Application:"* ]]; then
  echo "公証には Developer ID Application の署名が必要です" >&2
  exit 1
fi
