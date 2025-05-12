import json
import sys
import os

#!/usr/bin/env python3
"""
Script to read Jupyter notebook files and count words in markdown cells only.
"""


def count_words_in_markdown_cells(notebook_path):
    """Count words in markdown cells of a Jupyter notebook."""
    try:
        # Read the notebook file
        with open(notebook_path, 'r', encoding='utf-8') as f:
            notebook = json.load(f)
        
        # Get all markdown cells
        markdown_cells = [cell for cell in notebook.get('cells', []) 
                         if cell.get('cell_type') == 'markdown']
        
        if not markdown_cells:
            print(f"No markdown cells found in {notebook_path}")
            return 0
        
        # Initialize word count
        total_words = 0
        
        # Process each markdown cell
        for i, cell in enumerate(markdown_cells):
            # Get the source text
            source = ''.join(cell.get('source', []))
            
            # Count words
            words = source.split()
            word_count = len(words)
            
            # Add to total
            total_words += word_count
            
            # Print info for this cell
            print(f"Markdown cell #{i+1}: {word_count} words")
        
        return total_words
    
    except Exception as e:
        print(f"Error processing file {notebook_path}: {str(e)}")
        return 0

def main():
    if len(sys.argv) < 2:
        print("Usage: python word_counter.py [notebook_path]")
        return
    
    notebook_path = sys.argv[1]
    
    if not os.path.exists(notebook_path):
        print(f"Error: File {notebook_path} does not exist.")
        return
    
    # Count words in markdown cells
    total_words = count_words_in_markdown_cells(notebook_path)
    
    # Print total
    print(f"\nTotal word count in markdown cells: {total_words}")

if __name__ == "__main__":
    main()
    
    # Uncomment the following line for debugging purposes:
    # print(count_words_in_markdown_cells("/home/mitchell/Documents/Projects/P8-Project/Dataprocessing Pipeline/SPIS Project.ipynb"))