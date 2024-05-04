namespace DreamcastShortcutApp.DTO.Enum
{
    public enum IsoLoaderCddaModeEnum : uint
	{
		CDDA_MODE_DISABLED = 0,

		/* Simple mode selection */
		CDDA_MODE_DMA_TMU2 = 1,
		CDDA_MODE_DMA_TMU1 = 2,
		CDDA_MODE_SQ_TMU2 = 3,
		CDDA_MODE_SQ_TMU1 = 4,

		/* Extended mode selection */
		CDDA_MODE_EXTENDED = 5,
		CDDA_MODE_SRC_DMA = 0x00000010,
		CDDA_MODE_SRC_PIO = 0x00000020,
		CDDA_MODE_DST_DMA = 0x00000100,
		CDDA_MODE_DST_SQ = 0x00000200,
		CDDA_MODE_POS_TMU1 = 0x00001000,
		CDDA_MODE_POS_TMU2 = 0x00002000,
		CDDA_MODE_CH_ADAPT = 0x00010000,
		CDDA_MODE_CH_FIXED = 0x00020000
    }
}
