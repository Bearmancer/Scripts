/**
 * Merge Works Script
 * 
 * Combines consecutive rows with the same Work column into a single row.
 * Useful when movements are split across multiple rows.
 * 
 * Usage: Run from Extensions > Apps Script > mergeWorkRows
 */

/**
 * Creates a custom menu when the spreadsheet opens.
 */
function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('Music Tools')
    .addItem('Merge Work Rows', 'mergeWorkRows')
    .addToUi();
}

/**
 * Merges consecutive rows that share the same Work value.
 * Keeps first row's data, updates TrackEnd to last row's track number.
 */
function mergeWorkRows() {
  const sheet = SpreadsheetApp.getActiveSheet();
  const data = sheet.getDataRange().getValues();
  
  if (data.length < 2) {
    SpreadsheetApp.getUi().alert('Not enough data to merge');
    return;
  }
  
  const headers = data[0];
  const workCol = headers.indexOf('Work');
  const trackEndCol = headers.indexOf('TrackEnd');
  
  if (workCol === -1) {
    SpreadsheetApp.getUi().alert('Work column not found');
    return;
  }
  
  // Find rows to delete (merged into previous)
  const rowsToDelete = [];
  
  for (let i = data.length - 1; i > 1; i--) {
    const currentWork = data[i][workCol];
    const previousWork = data[i - 1][workCol];
    
    if (currentWork && currentWork === previousWork) {
      // Update previous row's TrackEnd if column exists
      if (trackEndCol !== -1) {
        data[i - 1][trackEndCol] = data[i][trackEndCol];
      }
      rowsToDelete.push(i + 1); // +1 for 1-indexed sheet rows
    }
  }
  
  // Delete rows from bottom up
  rowsToDelete.forEach(row => {
    sheet.deleteRow(row);
  });
  
  SpreadsheetApp.getUi().alert(`Merged ${rowsToDelete.length} rows`);
}
