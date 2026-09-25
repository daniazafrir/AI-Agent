import { describe, it, expect } from 'vitest';
import { highlightParts } from './source-highlight';

describe('source highlighting', () => {
  it('matches Hebrew and English words without losing whitespace', () => {
    const text = 'Vacation\nחופשה 19 days';
    const parts = highlightParts(text, 'vacation חופשה');
    expect(parts.filter(p => p.highlight).map(p => p.text)).toEqual(['Vacation', 'חופשה']);
    expect(parts.map(p => p.text).join('')).toBe(text);
  });
  it('does not highlight 19 inside 119', () => {
    expect(highlightParts('19 119', '19').filter(p => p.highlight).map(p => p.text)).toEqual(['19']);
  });
  it('treats regex syntax literally', () => {
    expect(highlightParts('C++ [a] .*', 'C++ [a] .*').filter(p => p.highlight).map(p => p.text)).toEqual(['C++', '[a]', '.*']);
  });
  it('keeps HTML-looking content as plain text', () => {
    const text = '<img src=x onerror=alert(1)>';
    expect(highlightParts(text, 'missing').map(p => p.text).join('')).toBe(text);
    expect(highlightParts(text, '')).toEqual([{ text, highlight: false }]);
  });
});
