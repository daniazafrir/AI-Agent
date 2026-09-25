# Chunk Viewer

Open a source underneath an assistant answer to load that document ID and
zero-based chunk index. The dialog shows the stored chunk, not the full document.
Source details show the retrieval score (RRF, not cosine similarity).

Numbers from the selected answer prefill the search field. Search terms are
whitespace-separated, case-insensitive literal words/numbers, including Hebrew.
The count is the number of occurrences. Clear removes highlights without
changing the text. Highlighting is lexical assistance, not evidence that the
model cited a particular span; translated answers may have no matching words.
Content is rendered with Angular text interpolation, never as document HTML.

## Browser acceptance

1. Ask the vacation question and open the source containing the vacation policy.
   Check the file name, chunk index, complete text and highlighted `19`.
2. Search `vacation`, then an absent word, and check counts; clear the field.
3. Open another source and a source from an older conversation. Confirm each
   dialog loads the selected source and derives its numbers from that answer.
4. Check keyboard focus, Escape/Close, scrolling, and a narrow mobile viewport.
5. Simulate a failed chunk GET through browser developer tools. Check that the
   spinner ends, an error appears, and retry works once the service is restored.
   A 404 indicates that the source is unavailable (deleted or reindexed).

## Validation

App Angular template compilation and seven direct Node checks passed.
Component tests cover safe text rendering, clearing highlights and 404/retry.
The local Angular test runner and production build are blocked by `spawn EPERM`;
these component tests must still run in an environment allowing build workers.
