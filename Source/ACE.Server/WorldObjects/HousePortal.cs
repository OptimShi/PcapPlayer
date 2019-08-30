using System;
using System.Linq;
using System.Numerics;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Database.Models.World;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.WorldObjects
{
    public sealed class HousePortal : Portal
    {
        public House House => ParentLink as House;

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public HousePortal(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public HousePortal(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        public override void SetLinkProperties(WorldObject wo)
        {
            // get properties from parent?
            wo.HouseId = House.HouseId;
            wo.HouseOwner = House.HouseOwner;
            wo.HouseInstance = House.HouseInstance;

            if (wo.IsLinkSpot)
            {
                var housePortals = House.GetHousePortals();
                if (housePortals.Count == 0)
                {
                    Console.WriteLine($"{Name}.SetLinkProperties({wo.Name}): found LinkSpot, but empty HousePortals");
                    return;
                }
                var i = housePortals[0];

                if (i.ObjCellId == Location.Cell && housePortals.Count > 1)
                    i = housePortals[1];

                var destination = new Position(i.ObjCellId, new Vector3(i.OriginX, i.OriginY, i.OriginZ), new Quaternion(i.AnglesX, i.AnglesY, i.AnglesZ, i.AnglesW));

                wo.SetPosition(PositionType.Destination, destination);

                // set portal destination directly?
                SetPosition(PositionType.Destination, destination);
            }
        }

        public override ActivationResult CheckUseRequirements(WorldObject activator)
        {
            if (!(activator is Player player))
                return new ActivationResult(false);

            if (player.CurrentLandblock.IsDungeon && Destination.LandblockId != player.CurrentLandblock.Id)
                return new ActivationResult(true); // Allow escape to overworld always.

            if (player.IgnorePortalRestrictions)
                return new ActivationResult(true);

            if (!House.RootHouse.HouseOwner.HasValue || House.RootHouse.HouseOwner == 0)
                return new ActivationResult(new GameEventWeenieError(player.Session, WeenieError.YouMustBeHouseGuestToUsePortal));

            if (House.RootHouse.IsOpen && House.RootHouse.HouseOwner.HasValue && House.RootHouse.HouseOwner > 0)
                return new ActivationResult(true);

            if (!House.RootHouse.HasPermission(player))
                return new ActivationResult(new GameEventWeenieError(player.Session, WeenieError.YouMustBeHouseGuestToUsePortal));

            return new ActivationResult(true);
        }

        /// <summary>
        /// House Portals are on Use activated, rather than collision based activation
        /// The actual portal process is wrapped to the base portal class ActOnUse, after ACL check are performed
        /// </summary>
        /// <param name="worldObject"></param>
        public override void ActOnUse(WorldObject worldObject)
        {
            // if house portal in dungeon,
            // set destination to outdoor house slumlord
            if (CurrentLandblock != null && CurrentLandblock.IsDungeon && Destination.LandblockId == CurrentLandblock.Id)
                SetPosition(PositionType.Destination, new Position(House.RootHouse.SlumLord.Location));

            base.ActOnUse(worldObject);
        }
    }
}