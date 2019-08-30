
namespace ACE.Adapter.Enum
{
    /// <summary>
    /// Weenie class IDs that are found in the dat file.
    /// This is just used as a helper for supplying the ClassName to weenie output
    /// </summary>
    enum WeenieClassId : ushort
    {
        UNDEF = 0,
        STORAGE = 9687,
        COINSTACK = 273,
        HUMAN = 1,
        ADMIN = 4,
        SENTINEL = 3648,
        BOOTSPOT = 10707,
        PORTALDESTINATION = 10762,
        ROBEENVOY = 26010,
        HELMENVOY = 26057,
        SHIELDENVOY = 26058,
        PLACEHOLDER = 3666,
        GATEKEEPER = 12303,
        DEED = 9549,
        BAELZHARON = 8503,
        ASHERON = 8675,
        PORTALMARKETPLACE = 23032,
        SKILLGEMDOWNARMORAPPRAISAL = 22317,
        SKILLGEMDOWNITEMAPPRAISAL = 22328,
        SKILLGEMDOWNMAGICITEMAPPRAISAL = 22337,
        SKILLGEMDOWNWEAPONAPPRAISAL = 22349,
        DOLLREWARDTUSKER = 9169,
        DOLLREWARDOLTHOI = 9170,
        DOLLREWARDMOSSWART = 9171,
        DOLLREWARDDRUDGE = 9172,
        DOLLREWARDURSUIN = 9173,
        DOLLREWARDLUGIAN = 9174,
        DOLLREWARDCOW = 9175,
        DOLLREWARDGRIEVVER = 9176,
        DOLLREWARDSCARECROW = 9177,
        DOLLREWARDVIRINDI = 9178,
        DOLLREWARDGOLEM = 9179,
        DOLLREWARDIDOL = 9180,
        DOLLREWARDASHERON = 29916,
        DOLLREWARDBAELZHARON = 29917,
        DOLLREWARDGAERLAN = 29918,
        DOLLREWARDKUKUUR = 29919,
        DOLLREWARDLEVISTRAS = 29920,
        DOLLREWARDMARTINE = 29921,
        DOLLREWARDOLTHOIQUEEN = 29922,
        CORPSE = 21,
        PORTALGATEWAY = 1955,
        ALLEGIANCE = 1149,
        EVENTCOORDINATOR = 6143,
        HUD = 9547,
        CHANNEL_ABUSE = 2346,
        CHANNEL_ADMIN = 2347,
        CHANNEL_AUDIT = 2348,
        CHANNEL_HELP = 2352,
        CHANNEL_ADVOCATE1 = 2349,
        CHANNEL_ADVOCATE2 = 2350,
        CHANNEL_ADVOCATE3 = 2351,
        CHANNEL_SENTINEL = 3654,
        CALLINGSTONE = 5084,
        BREAD = 259,
        APPLE = 258,
        VIAMONTTOAST = 4745,
        SWORDTRAINING = 12747,
        MACETRAINING = 12744,
        AXETRAINING = 12740,
        SPEARTRAINING = 12745,
        CESTUSTRAINING = 12742,
        STAFFTRAINING = 12743,
        DAGGERTRAINING = 12739,
        BOWTRAINING = 12741,
        XBOWTRAINING = 12749,
        ATLATLTRAINING = 12746,
        WANDTRAINING = 12748,
        LOCKPICKCRUDE = 511,
        HEALINGKITCRUDE = 628,
        FLOUR = 4761,
        WATER = 4746,
        ARROWSHAFT = 4585,
        QUARRELSHAFT = 5339,
        ATLATLDARTSHAFT = 15296,
        ARROWHEAD = 4586,
        MORTARANDPESTLE = 4751,
        GEMAZURITE = 2414,
        PACKCREATUREESSENCE = 15268,
        PACKITEMESSENCE = 15269,
        PACKLIFEESSENCE = 15270,
        PACKWARESSENCE = 15271,
        TAPERPRISMATIC = 20631,
        SCARABLEAD = 691,
        TINKERINGTOOL = 20646,
        SACK = 166,
        DRUDGEROOK = 14343,
        DRUDGEKNIGHT = 14344,
        DRUDGEBISHOP = 14345,
        DRUDGEQUEEN = 14346,
        DRUDGEKING = 14347,
        DRUDGEPAWN = 14342,
        MOSSWARTROOK = 14405,
        MOSSWARTKNIGHT = 14406,
        MOSSWARTBISHOP = 14407,
        MOSSWARTQUEEN = 14408,
        MOSSWARTKING = 14409,
        MOSSWARTPAWN = 14404,
        ORBHOMUNCULUS1 = 27649,
        CROPTUPEREA_XP = 10994,
        EXQUISITEELARIBOW_XP = 11001,
        SCALPELVIRINDI_XP = 11007,
        GEMACTDPURCHASEREWARDHEALTH = 31001,
        GEMACTDPURCHASEREWARDARMOR = 31000,
        NOTELETTERGREETINGALU = 30988,
        NOTELETTERGREETINGGHA = 30986,
        NOTELETTERGREETINGSHO = 30985,
        NOTELETTERGREETINGVIA = 30987,
        ARROWACADEMY = 31717,
        BOLTACADEMY = 31716,
        ATLATLDARTACADEMY = 31715,
        SOCIALMANAGER = 31869,
        PORTALPKARENANEW1 = 32575,
        PORTALPKARENANEW2 = 32576,
        PORTALPKARENANEW3 = 32577,
        PORTALPKARENANEW4 = 32578,
        PORTALPKARENANEW5 = 32579,
        PORTALPKLARENANEW1 = 32580,
        PORTALPKLARENANEW2 = 32581,
        PORTALPKLARENANEW3 = 32582,
        PORTALPKLARENANEW4 = 32583,
        PORTALPKLARENANEW5 = 32574,
        TOKENPATHWARDEN = 33613,
        TWOHANDEDTRAINING = 41512,
        GEARCRAFTARMORTOOL = 42979,
        NOTELETTERGREETINGSHA = 43019,
        NOTELETTERGREETINGGEAR = 43018,
        GEARCRAFTREVERSETOOL = 43022,
        PACKVOIDESSENCE = 43173,
        OLTHOIPLAYER = 43480,
        OLTHOIACIDPLAYER = 43481,
        OLTHOIADMIN = 43494,
        OLTHOIACIDADMIN = 43493,
        WANDOLTHOI = 43489,
        COINOLTHOI = 43491,
        HEALINGKITOLTHOI = 43701,
        OLTHOIMANAPOTION = 43635,
        OLTHOIHEALTHPOTION = 43634,
        OLTHOIPVPCURRENCY = 43747,
        BOOKCOMBATREVAMP = 45488,
        SWORDTRAININGFINESSE = 45553,
        MACETRAININGFINESSE = 45541,
        AXETRAININGFINESSE = 45533,
        SPEARTRAININGFINESSE = 45545,
        CESTUSTRAININGFINESSE = 45557,
        STAFFTRAININGFINESSE = 45549,
        DAGGERTRAININGFINESSE = 45537,
        SWORDTRAININGLIGHT = 45554,
        MACETRAININGLIGHT = 45542,
        AXETRAININGLIGHT = 45534,
        SPEARTRAININGLIGHT = 45546,
        CESTUSTRAININGLIGHT = 45558,
        STAFFTRAININGLIGHT = 45550,
        DAGGERTRAININGLIGHT = 45538,
        SHIELDROUND = 93,
        PETDEVICEGOLEMMUD = 48886,
    }
}
