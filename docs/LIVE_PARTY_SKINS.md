# Live Party Skins - feasibility record

## Goal

Each player chooses one local Minecraft skin (and, later, one cape drawing).
While friends play together, the client should show the matching drawing for
each individual player even if every player chose **Darian** in the Locker.
No one should need to install another player's PNG manually.

## What the current mod can and cannot do

The current app writes an Unreal IoStore mod before the game starts.  It
replaces a texture asset by asset path.  That is intentionally safe and is why
it works without a loader.

The Darian texture has one asset path, so replacing it means every Darian uses
the same image *on that client*.  A PAK cannot select a different texture for
each network player, and changing it after the game has started is not a safe
or reliable live-update mechanism.

Therefore, a party-download service alone does **not** solve Live Party Skins.
It needs an in-game adapter that can:

1. observe a stable party/player identity;
2. receive a normalized texture from the companion;
3. create/cache a game texture; and
4. assign that texture to that particular actor/material at runtime.

Nothing in this repository performs those operations today.

## Local reconnaissance (2026-09-30)

The installed Windows build has the normal shipping executable and game DLLs,
but no bundled `UE4SS`, `UE5SS`, `ModLoader`, Easy Anti-Cheat, or BattlEye file
was found by filename.  More importantly, there is currently no documented,
trusted mod-loader integration for this game in the project.  This is *not*
proof that an adapter is impossible; it is a stop sign for shipping or testing
DLL injection, memory hooks, or a loader guessed from Internet snippets.

The safe PAK path is already working and should remain the stable product.

## The safe companion protocol

Once a permitted in-game adapter exists, the companion can use this narrow
protocol.  It deliberately is not a general file-share feature.

| Field | Rule |
| --- | --- |
| Room | host creates a fresh, high-entropy join code for each session |
| Transport | LAN direct for the first prototype; Internet needs Steam-approved P2P or a small signalling/relay service |
| Asset types | `skin` and `cape` only |
| Skin payload | decoded and re-encoded 64x64 (or legacy 64x32 converted to 64x64) PNG |
| Cape payload | decoded and re-encoded PNG with an explicit maximum canvas size decided by the cape mapper |
| Size caps | max 256 KiB input, fixed decoded-pixel cap, and a hard per-session byte budget |
| Manifest | player id, asset kind, SHA-256, byte length, protocol version -- no paths, zip files, or arbitrary metadata |
| Cache | keyed only by SHA-256; never execute or open received files |
| Consent | every player must run the companion and approve joining the room |

The receiver must reject malformed images, oversized inputs, duplicate
conflicting hashes, unknown fields, and any message outside the short allow
list.  The data received over the network must never be written into the game
installation.  It belongs in an app cache, and the runtime adapter may consume
only its validated, normalized form.

## Delivery checkpoints

1. **Done:** static skins/outer layer through the existing PAK method.
2. **Next useful feature:** custom-cape mapper.  Override the texture of a
   cape the player already owns and equips; retain the game's mesh and cloth
   behaviour.  A numbered diagnostic texture is needed to map its UVs.
3. **Research gate:** find a documented, permitted UE5 modding extension that
   exposes actor identity and material assignment for this exact build.
4. **Only after the gate:** make a loopback/LAN-only Party Sync prototype with
   the protocol above, then test it with two PCs and two consenting accounts.
5. **Internet phase:** choose Steam-supported P2P if it is available to the
   companion; otherwise provide an explicit relay/signalling option.  A host
   behind NAT cannot reliably accept arbitrary Internet peers without one of
   those mechanisms or deliberate port forwarding.

## Explicit non-goals

- Do not unlock official capes or bypass ownership checks.
- Do not send executable files, PAKs, archives, filesystem paths, or arbitrary
  blobs through a party room.
- Do not silently modify game files while the game is running.
- Do not attempt undocumented process injection or network/gameplay hooks.

## Current decision

Live Party Skins is a viable product idea, but it is blocked at checkpoint 3,
not at networking.  Building a server now would create a secure-looking room
that cannot make the game render per-player skins.  The highest-value next
implementation is the custom-cape mapper while loader support is researched.
