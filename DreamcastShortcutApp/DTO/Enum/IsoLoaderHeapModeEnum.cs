namespace DreamcastShortcutApp.DTO.Enum
{
	public enum IsoLoaderHeapModeEnum : uint
    {
		HEAP_MODE_AUTO = 0,
		HEAP_MODE_BEHIND = 1,
		HEAP_MODE_INGAME,
		HEAP_MODE_MAPLE,
		HEAP_MODE_SPECIFY = 0x8c000000
    }
}
