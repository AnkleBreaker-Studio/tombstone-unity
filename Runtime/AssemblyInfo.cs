using System.Runtime.CompilerServices;

// The editor assembly reuses internal helpers (TombstoneJson) for request bodies.
// No wire shapes change here — tests/unity-contract.test.ts stays authoritative.
[assembly: InternalsVisibleTo("AnkleBreaker.Tombstone.Editor")]
