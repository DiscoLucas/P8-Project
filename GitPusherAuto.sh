#!/bin/bash
set -e

BATCH_SIZE=1
BRANCH="fucjk-backup"  # change if needed

# Get the total number of changed files at the start.
TOTAL_CHANGES=$(git status --porcelain | wc -l)
echo "Total changed files: $TOTAL_CHANGES"

COMMITTED=0

while true; do
  FILES_TO_ADD=()
  COUNT=0
  
  # Read changed files in a safe, null-terminated way.
  while IFS= read -r -d '' LINE; do
    # The file path starts at position 4 (after the two status characters and a space).
    FILE="${LINE:3}"
    FILES_TO_ADD+=("$FILE")
    COUNT=$((COUNT+1))
    if [ $COUNT -ge $BATCH_SIZE ]; then
      break
    fi
  done < <(git status --porcelain -z)
  
  # If no files left, exit.
  if [ ${#FILES_TO_ADD[@]} -eq 0 ]; then
    echo "All changes committed and pushed."
    exit 0
  fi

  echo "Staging the following files:"
  for FILE in "${FILES_TO_ADD[@]}"; do
    echo "$FILE"
    git add "$FILE"
  done

  COMMITTED=$((COMMITTED + COUNT))
  
  # Commit with a progress message.
  git commit -m "Batch commit: committed $COMMITTED of $TOTAL_CHANGES changes"
  
  echo "Pushing commit..."
  if ! git push origin "$BRANCH"; then
    echo "Push failed. Stopping the script."
    exit 1
  fi
  
  echo "Successfully pushed batch. Progress: $COMMITTED/$TOTAL_CHANGES changes committed."
done
