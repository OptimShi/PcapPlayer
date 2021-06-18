using ACE.PcapReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CM_Movement : MessageProcessor {

    public override bool acceptMessageData(BinaryReader messageDataReader) {
        bool handled = true;

        PacketOpcode opcode = Util.readOpcode(messageDataReader);
        switch (opcode) {
            case PacketOpcode.Evt_Movement__PositionAndMovement:
                {
                    PositionAndMovement message = PositionAndMovement.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__Jump_ID: {
                    Jump message = Jump.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__MoveToState_ID: {
                    MoveToState message = MoveToState.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__DoMovementCommand_ID: {
                    DoMovementCommand message = DoMovementCommand.read(messageDataReader);
                    break;
                }
            // TODO: PacketOpcode.Evt_Movement__TurnEvent_ID
            // TODO: PacketOpcode.Evt_Movement__TurnToEvent_ID
            case PacketOpcode.Evt_Movement__StopMovementCommand_ID: {
                    StopMovementCommand message = StopMovementCommand.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__UpdatePosition_ID: {
                    UpdatePosition message = UpdatePosition.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__MovementEvent_ID: {
                    MovementEvent message = MovementEvent.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__AutonomyLevel_ID: {
                    AutonomyLevel message = AutonomyLevel.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__AutonomousPosition_ID: {
                    AutonomousPosition message = AutonomousPosition.read(messageDataReader);
                    break;
                }
            case PacketOpcode.Evt_Movement__Jump_NonAutonomous_ID: {
                    Jump_NonAutonomous message = Jump_NonAutonomous.read(messageDataReader);
                    break;
                }
            default: {
                    handled = false;
                    break;
                }
        }

        return handled;
    }

    public class Position
    {
        public uint objcell_id;
        public float x, y, z;
        public float qw, qx, qy, qz;
        public int Length;

        public static Position read(BinaryReader binaryReader)
        {
            Position newObj = new Position();
            var startPosition = binaryReader.BaseStream.Position;
            newObj.objcell_id = binaryReader.ReadUInt32();
            newObj.x = binaryReader.ReadSingle();
            newObj.y = binaryReader.ReadSingle();
            newObj.z = binaryReader.ReadSingle();
            newObj.qw = binaryReader.ReadSingle();
            newObj.qx = binaryReader.ReadSingle();
            newObj.qy = binaryReader.ReadSingle();
            newObj.qz = binaryReader.ReadSingle();
            newObj.Length = (int)(binaryReader.BaseStream.Position - startPosition);
            return newObj;
        }

        public static Position readOrigin(BinaryReader binaryReader)
        {
            Position newObj = new Position();
            var startPosition = binaryReader.BaseStream.Position;
            newObj.objcell_id = binaryReader.ReadUInt32();
            newObj.x = binaryReader.ReadSingle();
            newObj.y = binaryReader.ReadSingle();
            newObj.z = binaryReader.ReadSingle();
            newObj.Length = (int)(binaryReader.BaseStream.Position - startPosition);
            return newObj;
        }

    }

    public class JumpPack {
        public float extent;
        public Position position;
        public ushort instance_timestamp;
        public ushort server_control_timestamp;
        public ushort teleport_timestamp;
        public ushort force_position_ts;

        public static JumpPack read(BinaryReader binaryReader) {
            JumpPack newObj = new JumpPack();
            newObj.extent = binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            binaryReader.ReadSingle();
            newObj.position = Position.read(binaryReader);
            newObj.instance_timestamp = binaryReader.ReadUInt16();
            newObj.server_control_timestamp = binaryReader.ReadUInt16();
            newObj.teleport_timestamp = binaryReader.ReadUInt16();
            newObj.force_position_ts = binaryReader.ReadUInt16();
            Util.readToAlign(binaryReader);
            return newObj;
        }
    }

    public class Jump : Message {
        public JumpPack i_jp;

        public static Jump read(BinaryReader binaryReader) {
            Jump newObj = new Jump();
            newObj.i_jp = JumpPack.read(binaryReader);
            return newObj;
        }
    }

    public class MoveToState : Message {
        public RawMotionState raw_motion_state;
        public Position position;
        public ushort instance_timestamp;
        public ushort server_control_timestamp;
        public ushort teleport_timestamp;
        public ushort force_position_ts;
        public bool contact;
        public bool longjump_mode;

        public static MoveToState read(BinaryReader binaryReader) {
            MoveToState newObj = new MoveToState();
            newObj.raw_motion_state = RawMotionState.read(binaryReader);
            newObj.position = Position.read(binaryReader);
            newObj.instance_timestamp = binaryReader.ReadUInt16();
            newObj.server_control_timestamp = binaryReader.ReadUInt16();
            newObj.teleport_timestamp = binaryReader.ReadUInt16();
            newObj.force_position_ts = binaryReader.ReadUInt16();

            byte flags = binaryReader.ReadByte();

            newObj.contact = (flags & (1 << 0)) != 0;
            newObj.longjump_mode = (flags & (1 << 1)) != 0;

            Util.readToAlign(binaryReader);

            return newObj;
        }
    }

    public class DoMovementCommand : Message {
        public uint i_motion;
        public float i_speed;
        public uint i_hold_key;

        public static DoMovementCommand read(BinaryReader binaryReader) {
            DoMovementCommand newObj = new DoMovementCommand();
            newObj.i_motion = binaryReader.ReadUInt32();
            Util.readToAlign(binaryReader);
            newObj.i_speed = binaryReader.ReadSingle();
            Util.readToAlign(binaryReader);
            newObj.i_hold_key = binaryReader.ReadUInt32();
            Util.readToAlign(binaryReader);
            return newObj;
        }
    }

    public class StopMovementCommand : Message {
        public uint i_motion;
        public uint i_hold_key;

        public static StopMovementCommand read(BinaryReader binaryReader) {
            StopMovementCommand newObj = new StopMovementCommand();
            newObj.i_motion = binaryReader.ReadUInt32();
            Util.readToAlign(binaryReader);
            newObj.i_hold_key = binaryReader.ReadUInt32();
            Util.readToAlign(binaryReader);
            return newObj;
        }
    }

    public class PositionPack
    {
        public enum PackBitfield
        {
            HasVelocity       = 0x1,
            HasPlacementID    = 0x2,
            IsGrounded        = 0x4,
            OrientationHasNoW = 0x8,
            OrientationHasNoX = 0x10,
            OrientationHasNoY = 0x20,
            OrientationHasNoZ = 0x40,
        }

        public uint bitfield;
        public Position position;
        public float velocity_x;
        public float velocity_y;
        public float velocity_z;
        public uint placement_id;
        public bool has_contact;
        public ushort instance_timestamp;
        public ushort position_timestamp;
        public ushort teleport_timestamp;
        public ushort force_position_timestamp;
        public List<string> packedItems; // For display purposes

        public static PositionPack read(BinaryReader binaryReader)
        {
            PositionPack newObj = new PositionPack();
            newObj.packedItems = new List<string>();
            newObj.bitfield = binaryReader.ReadUInt32();
            newObj.position = Position.readOrigin(binaryReader);

            if ((newObj.bitfield & (uint)PackBitfield.OrientationHasNoW) == 0)
            {
                newObj.position.qw = binaryReader.ReadSingle();
            }
            else
            {
                newObj.packedItems.Add(PackBitfield.OrientationHasNoW.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.OrientationHasNoX) == 0)
            {
                newObj.position.qx = binaryReader.ReadSingle();
            }
            else
            {
                newObj.packedItems.Add(PackBitfield.OrientationHasNoX.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.OrientationHasNoY) == 0)
            {
                newObj.position.qy = binaryReader.ReadSingle();
            }
            else
            {
                newObj.packedItems.Add(PackBitfield.OrientationHasNoY.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.OrientationHasNoZ) == 0)
            {
                newObj.position.qz = binaryReader.ReadSingle();
            }
            else
            {
                newObj.packedItems.Add(PackBitfield.OrientationHasNoZ.ToString());
            }

           // newObj.position.frame.cache();

            if ((newObj.bitfield & (uint)PackBitfield.HasVelocity) != 0)
            {
                newObj.velocity_x = binaryReader.ReadSingle();
                newObj.velocity_y = binaryReader.ReadSingle();
                newObj.velocity_z = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.HasVelocity.ToString());
            }

            if ((newObj.bitfield & (uint)PackBitfield.HasPlacementID) != 0)
            {
                newObj.placement_id = binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.HasPlacementID.ToString());
            }

            newObj.has_contact = (newObj.bitfield & (uint)PackBitfield.IsGrounded) != 0;
            if (newObj.has_contact)
                newObj.packedItems.Add(PackBitfield.IsGrounded.ToString());

            newObj.instance_timestamp = binaryReader.ReadUInt16();
            newObj.position_timestamp = binaryReader.ReadUInt16();
            newObj.teleport_timestamp = binaryReader.ReadUInt16();
            newObj.force_position_timestamp = binaryReader.ReadUInt16();
            return newObj;
        }
    }

    public class UpdatePosition : Message {
        public uint object_id;
        public PositionPack positionPack;

        public static UpdatePosition read(BinaryReader binaryReader) {
            UpdatePosition newObj = new UpdatePosition();
            newObj.object_id = binaryReader.ReadUInt32();
            newObj.positionPack = PositionPack.read(binaryReader);
            return newObj;
        }
    }

    public class InterpretedMotionState {
        public enum PackBitfield {
            current_style = (1 << 0),
            forward_command = (1 << 1),
            forward_speed = (1 << 2),
            sidestep_command = (1 << 3),
            sidestep_speed = (1 << 4),
            turn_command = (1 << 5),
            turn_speed = (1 << 6)
        }

        public uint bitfield;
        public uint current_style;
        public uint forward_command;
        public uint sidestep_command;
        public uint turn_command;
        public float forward_speed = 1.0f;
        public float sidestep_speed = 1.0f;
        public float turn_speed = 1.0f;
        public List<ActionNode> actions = new List<ActionNode>();
        public List<string> packedItems = new List<string>(); // For display purposes

        public static InterpretedMotionState read(BinaryReader binaryReader) {
            InterpretedMotionState newObj = new InterpretedMotionState();
            newObj.bitfield = binaryReader.ReadUInt32();
            if ((newObj.bitfield & (uint)PackBitfield.current_style) != 0) {
                newObj.current_style = (uint)command_ids[binaryReader.ReadUInt16()];
                newObj.packedItems.Add(PackBitfield.current_style.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.forward_command) != 0) {
                newObj.forward_command = (uint)command_ids[binaryReader.ReadUInt16()];
                newObj.packedItems.Add(PackBitfield.forward_command.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.sidestep_command) != 0) {
                newObj.sidestep_command = (uint)command_ids[binaryReader.ReadUInt16()];
                newObj.packedItems.Add(PackBitfield.sidestep_command.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.turn_command) != 0) {
                newObj.turn_command = (uint)command_ids[binaryReader.ReadUInt16()];
                newObj.packedItems.Add(PackBitfield.turn_command.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.forward_speed) != 0) {
                newObj.forward_speed = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.forward_speed.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.sidestep_speed) != 0) {
                newObj.sidestep_speed = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.sidestep_speed.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.turn_speed) != 0) {
                newObj.turn_speed = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.turn_speed.ToString());
            }

            uint num_actions = (newObj.bitfield >> 7) & 0x1F;
            newObj.packedItems.Add("num_actions = " + num_actions);
            for (int i = 0; i < num_actions; ++i) {
                newObj.actions.Add(ActionNode.read(binaryReader));
            }

            Util.readToAlign(binaryReader);

            return newObj;
        }
    }

    public class ActionNode
    {
        public uint action;
        public uint stamp;
        public int autonomous;
        public float speed;

        public static ActionNode read(BinaryReader binaryReader)
        {
            ActionNode newObj = new ActionNode();
            newObj.action = command_ids[binaryReader.ReadUInt16()];
            uint packedSequence = binaryReader.ReadUInt16();
            newObj.stamp = packedSequence & 0x7FFF;
            newObj.autonomous = (int)((packedSequence >> 15) & 1);
            newObj.speed = binaryReader.ReadSingle();
            return newObj;
        }
    }

    public class MovementParameters {
        public enum PackBitfield
        {
            can_walk = (1 << 0),
            can_run = (1 << 1),
            can_sidestep = (1 << 2),
            can_walk_backwards = (1 << 3),
            can_charge = (1 << 4),
            fail_walk = (1 << 5),
            use_final_heading = (1 << 6),
            sticky = (1 << 7),
            move_away = (1 << 8),
            move_towards = (1 << 9),
            use_spheres = (1 << 10),
            set_hold_key = (1 << 11),
            autonomous = (1 << 12),
            modify_raw_state = (1 << 13),
            modify_interpreted_state = (1 << 14),
            cancel_moveto = (1 << 15),
            stop_completely = (1 << 16),
            disable_jump_during_link = (1 << 17),
        }

        public uint bitfield;
        public float distance_to_object;
        public float min_distance;
        public float fail_distance;
        public float speed;
        public float walk_run_threshhold;
        public float desired_heading;

        public static MovementParameters read(uint type, BinaryReader binaryReader) {
            MovementParameters newObj = new MovementParameters();
            switch (type) {
                case 6:
                case 7: {
                        newObj.bitfield = binaryReader.ReadUInt32();
                        newObj.distance_to_object = binaryReader.ReadSingle();
                        newObj.min_distance = binaryReader.ReadSingle();
                        newObj.fail_distance = binaryReader.ReadSingle();
                        newObj.speed = binaryReader.ReadSingle();
                        newObj.walk_run_threshhold = binaryReader.ReadSingle();
                        newObj.desired_heading = binaryReader.ReadSingle();
                        break;
                    }
                case 8:
                case 9: {
                        newObj.bitfield = binaryReader.ReadUInt32();
                        newObj.speed = binaryReader.ReadSingle();
                        newObj.desired_heading = binaryReader.ReadSingle();
                        break;
                    }
                default: {
                        break;
                    }
            }
            return newObj;
        }
    }

    // This class does not appear in the client but is added for convenience
    public class MovementData
    {
        public ushort movement_timestamp;
        public ushort server_control_timestamp;
        public byte autonomous;
        public MovementDataUnpack movementData_Unpack;

        public static MovementData read(BinaryReader binaryReader)
        {
            MovementData newObj = new MovementData();
            newObj.movement_timestamp = binaryReader.ReadUInt16();
            newObj.server_control_timestamp = binaryReader.ReadUInt16();
            newObj.autonomous = binaryReader.ReadByte();

            Util.readToAlign(binaryReader);

            newObj.movementData_Unpack = MovementDataUnpack.read(binaryReader);
            return newObj;
        }
    }

    // A class that mimics MovementManager::unpack_movement
    public class MovementDataUnpack
    {
        public uint movement_type;
        public ushort movement_options;
        public MovementParameters movement_params = new MovementParameters();
        public uint style;
        public InterpretedMotionState interpretedMotionState = new InterpretedMotionState();
        public uint stickToObject;
        public bool standing_longjump = false;
        public uint moveToObject;
        public Position moveToPos = new Position();
        public float my_run_rate;
        public uint turnToObject;
        public float desiredHeading;

        public static MovementDataUnpack read(BinaryReader binaryReader)
        {
            MovementDataUnpack newObj = new MovementDataUnpack();
            ushort pack_word = binaryReader.ReadUInt16();
            newObj.movement_options = (ushort)(pack_word & 0xFF00);
            newObj.movement_type = (uint)((ushort)(pack_word & 0x00FF));
            newObj.style = (uint)command_ids[binaryReader.ReadUInt16()];
            switch (newObj.movement_type)
            {
                case 0:
                    {
                        newObj.interpretedMotionState = InterpretedMotionState.read(binaryReader);
                        if ((newObj.movement_options & 0x100) != 0)
                        {
                            newObj.stickToObject = binaryReader.ReadUInt32();
                        }
                        if ((newObj.movement_options & 0x200) != 0)
                        {
                            newObj.standing_longjump = true;
                        }
                        break;
                    }
                case 6:
                    {
                        newObj.moveToObject = binaryReader.ReadUInt32();
                        newObj.moveToPos = Position.readOrigin(binaryReader);
                        newObj.movement_params = MovementParameters.read(newObj.movement_type, binaryReader);
                        newObj.my_run_rate = binaryReader.ReadSingle();
                        break;
                    }
                case 7:
                    {
                        newObj.moveToPos = Position.readOrigin(binaryReader);
                        newObj.movement_params = MovementParameters.read(newObj.movement_type, binaryReader);
                        newObj.my_run_rate = binaryReader.ReadSingle();
                        break;
                    }
                case 8:
                    {
                        newObj.turnToObject = binaryReader.ReadUInt32();
                        newObj.desiredHeading = binaryReader.ReadSingle();
                        newObj.movement_params = MovementParameters.read(newObj.movement_type, binaryReader);
                        break;
                    }
                case 9:
                    {
                        newObj.movement_params = MovementParameters.read(newObj.movement_type, binaryReader);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            return newObj;
        }
    }

    public class MovementEvent : Message {
        public uint object_id;
        public ushort instance_timestamp;
        public MovementData movement_data;

        public static MovementEvent read(BinaryReader binaryReader) {
            MovementEvent newObj = new MovementEvent();
            newObj.object_id = binaryReader.ReadUInt32();
            newObj.instance_timestamp = binaryReader.ReadUInt16();
            newObj.movement_data = MovementData.read(binaryReader);

            return newObj;
        }
    }

    public class AutonomyLevel : Message {
        public uint i_autonomy_level;

        public static AutonomyLevel read(BinaryReader binaryReader) {
            AutonomyLevel newObj = new AutonomyLevel();
            newObj.i_autonomy_level = binaryReader.ReadUInt32();
            Util.readToAlign(binaryReader);
            return newObj;
        }
    }

    public class RawMotionState {
        public enum PackBitfield {
            current_holdkey = (1 << 0),
            current_style = (1 << 1),
            forward_command = (1 << 2),
            forward_holdkey = (1 << 3),
            forward_speed = (1 << 4),
            sidestep_command = (1 << 5),
            sidestep_holdkey = (1 << 6),
            sidestep_speed = (1 << 7),
            turn_command = (1 << 8),
            turn_holdkey = (1 << 9),
            turn_speed = (1 << 10),
        }

        public uint bitfield;
        public List<string> packedItems; // For display purposes
        public uint current_holdkey;
        public uint current_style;
        public uint forward_command;
        public uint forward_holdkey;
        public float forward_speed = 1.0f;
        public uint sidestep_command;
        public uint sidestep_holdkey;
        public float sidestep_speed = 1.0f;
        public uint turn_command;
        public uint turn_holdkey;
        public float turn_speed = 1.0f;
        public List<ActionNode> actions;

        public static RawMotionState read(BinaryReader binaryReader)
        {
            RawMotionState newObj = new RawMotionState();
            newObj.packedItems = new List<string>();
            newObj.bitfield = binaryReader.ReadUInt32();
            if ((newObj.bitfield & (uint)PackBitfield.current_holdkey) != 0)
            {
                newObj.current_holdkey = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.current_holdkey.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.current_style) != 0)
            {
                newObj.current_style = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.current_style.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.forward_command) != 0)
            {
                newObj.forward_command = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.forward_command.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.forward_holdkey) != 0)
            {
                newObj.forward_holdkey = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.forward_holdkey.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.forward_speed) != 0)
            {
                newObj.forward_speed = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.forward_speed.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.sidestep_command) != 0)
            {
                newObj.sidestep_command = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.sidestep_command.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.sidestep_holdkey) != 0)
            {
                newObj.sidestep_holdkey = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.sidestep_holdkey.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.sidestep_speed) != 0)
            {
                newObj.sidestep_speed = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.sidestep_speed.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.turn_command) != 0)
            {
                newObj.turn_command = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.turn_command.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.turn_holdkey) != 0)
            {
                newObj.turn_holdkey = (uint)binaryReader.ReadUInt32();
                newObj.packedItems.Add(PackBitfield.turn_holdkey.ToString());
            }
            if ((newObj.bitfield & (uint)PackBitfield.turn_speed) != 0)
            {
                newObj.turn_speed = binaryReader.ReadSingle();
                newObj.packedItems.Add(PackBitfield.turn_speed.ToString());
            }

            uint num_actions = (newObj.bitfield >> 11);
            newObj.packedItems.Add("num_actions = " + num_actions);
            newObj.actions = new List<ActionNode>();
            for (int i = 0; i < num_actions; ++i)
            {
                newObj.actions.Add(ActionNode.read(binaryReader));
            }

            Util.readToAlign(binaryReader);

            return newObj;
        }
    }

    public class AutonomousPosition : Message {
        public Position position;
        public ushort instance_timestamp;
        public ushort server_control_timestamp;
        public ushort teleport_timestamp;
        public ushort force_position_timestamp;
        public bool contact;

        public static AutonomousPosition read(BinaryReader binaryReader) {
            AutonomousPosition newObj = new AutonomousPosition();

            newObj.position = Position.read(binaryReader);

            newObj.instance_timestamp = binaryReader.ReadUInt16();
            newObj.server_control_timestamp = binaryReader.ReadUInt16();
            newObj.teleport_timestamp = binaryReader.ReadUInt16();
            newObj.force_position_timestamp = binaryReader.ReadUInt16();

            newObj.contact = binaryReader.ReadByte() != 0;

            Util.readToAlign(binaryReader);

            return newObj;
        }
    }

    public class Jump_NonAutonomous : Message {
        public float i_extent;

        public static Jump_NonAutonomous read(BinaryReader binaryReader) {
            Jump_NonAutonomous newObj = new Jump_NonAutonomous();
            newObj.i_extent = binaryReader.ReadSingle();
            Util.readToAlign(binaryReader);
            return newObj;
        }
    }

    public class PositionAndMovement : Message
    {
        public uint object_id;
        public PositionPack positionPack;
        public MovementData movementData;

        public static PositionAndMovement read(BinaryReader binaryReader)
        {
            PositionAndMovement newObj = new PositionAndMovement();
            newObj.object_id = binaryReader.ReadUInt32();
            newObj.positionPack = PositionPack.read(binaryReader);
            newObj.movementData = MovementData.read(binaryReader);

            return newObj;
        }
    }

    // Note: These IDs are from the last version of the client. Earlier versions of the client had a different array order and values.
    static uint[] command_ids = {

    };
}
