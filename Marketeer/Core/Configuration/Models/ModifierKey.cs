using System;

namespace Marketeer.Core.Configuration.Models;

[Flags]
public enum ModifierKey {
    None = 0,
    Ctrl = 1 << 0,
    Alt = 1 << 1,
    Shift = 1 << 2
}