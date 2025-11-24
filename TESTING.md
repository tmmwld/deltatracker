# Testing Guide

Since you're on macOS, you won't be able to run the application locally. However, here's how to test once it's built on Windows:

## Creating Test Screenshots

To test the OCR functionality, you'll need screenshots from Delta Force showing the balance.

### English Version Example
The screenshot should show something like:
```
Total Assets    902.4M
```

### Russian Version Example
The screenshot should show:
```
Общие активы    902,4М
```

## Testing Workflow with GitHub Actions

1. **Push the code to GitHub**:
   ```bash
   cd /Users/vadim/.gemini/antigravity/scratch/DeltaForceTracker
   git init
   git add .
   git commit -m "Initial commit: Delta Force Balance Tracker"
   git branch -M main
   git remote add origin <your-repo-url>
   git push -u origin main
   ```

2. **GitHub Actions will automatically build**:
   - Go to your repository's Actions tab
   - Wait for the build to complete
   - Download the artifact

3. **Transfer to Windows machine**:
   - Extract the artifact
   - Run `DeltaForceTracker.exe`

## Manual Testing Checklist

Once you have the app running on Windows:

### Initial Setup Test
- [ ] Application launches without errors
- [ ] Main window displays correctly
- [ ] Dashboard shows "No scans yet"

### OCR Region Selection Test
- [ ] Click "Select OCR Region"
- [ ] Full-screen overlay appears
- [ ] Can drag to select a region
- [ ] Selection is saved

### Hotkey Test
- [ ] F8 hotkey is registered on startup
- [ ] Pressing F8 triggers a scan
- [ ] Can change hotkey via settings
- [ ] New hotkey works

### Scanning Test
- [ ] Create a test image with "Total Assets 500M"
- [ ] Place it on screen
- [ ] Select region over the image
- [ ] Press F8
- [ ] Balance is detected and recorded

### Data Persistence Test
- [ ] Scan a balance
- [ ] Close the application
- [ ] Reopen the application
- [ ] Previous scan is still visible
- [ ] Chart shows historical data

### P&L Calculation Test
- [ ] First scan of the day: 100M
- [ ] Second scan: 150M
- [ ] Daily P&L shows +50M in green
- [ ] Third scan: 90M
- [ ] Daily P&L shows -10M in red

### Analytics Test
- [ ] Scan multiple balances over different days
- [ ] Check "Best Day" shows highest P&L
- [ ] Check "Worst Day" shows lowest P&L
- [ ] Check "Highest Balance" shows peak
- [ ] History table shows all scans

## Sample Test Data

If you want to manually insert test data into the SQLite database:

```sql
INSERT INTO Scans (Timestamp, RawValue, NumericValue, DailyStartingBalance) VALUES
  ('2025-01-20 10:00:00', '500M', 500000000, 500000000),
  ('2025-01-20 14:00:00', '520.5M', 520500000, 500000000),
  ('2025-01-20 18:00:00', '495M', 495000000, 500000000),
  ('2025-01-21 09:00:00', '510M', 510000000, 510000000),
  ('2025-01-21 16:00:00', '550M', 550000000, 510000000);
```

This creates:
- Day 1: Start 500M, end 495M (P&L: -5M)
- Day 2: Start 510M, end 550M (P&L: +40M)
- Best day: +40M
- Worst day: -5M
- Highest balance: 550M

## Common Issues During Testing

### Issue: "tessdata folder not found"
**Solution**: Ensure tessdata folder with eng.traineddata and rus.traineddata is next to the .exe

### Issue: "OCR cannot read balance"
**Possible causes**:
- Text too small or blurry
- Selected region doesn't contain the label
- Font is not standard (use clean screenshot)

### Issue: "Hotkey not working"
**Solutions**:
- Run as Administrator
- Try a different key
- Check if another app is using the same hotkey

## Performance Testing

- OCR scan should complete in < 2 seconds
- Database queries should be instant
- UI should remain responsive during scans
- Memory usage should stay under 200 MB

---

For automated testing, consider creating unit tests for:
- `ValueParser.ParseBalanceString()` - various input formats
- Database CRUD operations
- P&L calculation logic
