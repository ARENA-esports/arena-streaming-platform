const fs = require('fs');
const html = fs.readFileSync('d:/arena/codebase/arena-dashboard.html', 'utf-8');
// Extract the <style> block
const match = html.match(/<style>([\s\S]*?)<\/style>/);
if (match) {
  let css = match[1];
  // Remove the giant base64 image to keep the CSS clean
  css = css.replace(/--stream-thumb:\s*url\([^)]+\);/, '--stream-thumb: url("");');
  
  let indexCss = fs.readFileSync('src/index.css', 'utf-8');
  // Find where the :root starts in indexCss and replace everything after it with the new CSS
  const rootIndex = indexCss.indexOf(':root {');
  if (rootIndex !== -1) {
    indexCss = indexCss.substring(0, rootIndex) + css;
  } else {
    indexCss += '\n' + css;
  }
  
  fs.writeFileSync('src/index.css', indexCss);
  console.log('Successfully injected CSS into index.css');
} else {
  console.log('Failed to find <style> block');
}
