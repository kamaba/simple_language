# =========================================================================
# Platform_Defs — definition registry (§13.3): DefItem / DefKind / defs.
#
# Part of `namespace Environment { namespace Platform { ... } }`
# (design doc PLATFORM_CAPABILITY_DESIGN.md §13.2).
# The registry data lives on the CVM side (sl_platform_def.c), mirrored
# from the enum definitions above — one ruler for Front jsonc validation
# and CVM environment comparison (§13.1).
#
# Usage:
#   if Environment.Platform.defs.contains( DefKind.OS, "linux" ) { ... }
#   DefItem item = Environment.Platform.defs.lookup( DefKind.OS, "linux" )
# =========================================================================

namespace Environment
{
    namespace Platform
    {
        # Single definition member descriptor.
        public class DefItem extends Object
        {
            # Enum value (must match CVM sl_platform_def.h).
            public Int32 value = 0
            # Member name (== jsonc key), e.g. "linux".
            public string name = ""
            # Alias list, e.g. ["win", "win64"] (accepted by CVM).
            public Array<string> aliases = Array<string>( 0 )
            # Human readable description (diagnostics).
            public string describe = ""
        }

        # Definition kinds (§13.3). Auto-increment values.
        public enum DefKind extends int
        {
            OS          = 0
            OSVersion   = 1
            Arch        = 2
            Cpu         = 3
            ISA         = 4
            Device      = 5
            AI          = 6
            Gfx         = 7
            ShaderModel = 8
            Network     = 9
            Link        = 10
            McuFamily   = 11
            Rtos        = 12
            Runtime     = 13
            BuildMode   = 14
            Endian      = 15
        }

        # ★ Registry — table data provided by C side (sl_platform_def.c).
        public class defs extends Object
        {
            # Look up a definition by kind + member name.
            # Returns the DefItem, or null when the name is unknown.
            public static DefItem lookup( DefKind kind, string name )
            {
                Int32 v = SystemPlatformDefLookup( kind, name )
                if v < 0
                {
                    ret null
                }
                DefItem item = DefItem()
                item.value = v
                item.name = name
                ret item
            }

            # Whether the definition table of the given kind contains name.
            public static bool contains( DefKind kind, string name )
            {
                ret SystemPlatformDefContains( kind, name )
            }

            # All member names (jsonc keys) of the given kind, in enum order.
            public static Array<string> names( DefKind kind )
            {
                Array<string> r = null
                r = SystemPlatformDefNames( kind )
                ret r
            }
        }
    }
}
