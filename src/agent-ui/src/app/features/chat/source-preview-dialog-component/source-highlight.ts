export function highlightParts(content: string, search: string): { text: string; highlight: boolean }[] {
  const terms = [...new Set(search.trim().split(/\s+/).filter(Boolean))]
    .sort((a, b) => b.length - a.length)
    .map(term => term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));
  if (!terms.length) return [{ text: content, highlight: false }];
  const pattern = new RegExp(`(?<![\\p{L}\\p{N}_])(?:${terms.join('|')})(?![\\p{L}\\p{N}_])`, 'giu');
  const parts: { text: string; highlight: boolean }[] = [];
  let cursor = 0;
  for (const match of content.matchAll(pattern)) {
    const index = match.index!;
    if (index > cursor) parts.push({ text: content.slice(cursor, index), highlight: false });
    parts.push({ text: match[0], highlight: true });
    cursor = index + match[0].length;
  }
  if (cursor < content.length) parts.push({ text: content.slice(cursor), highlight: false });
  return parts;
}
