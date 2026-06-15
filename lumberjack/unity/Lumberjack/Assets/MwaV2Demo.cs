// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Solana.Unity.Programs;
// using Solana.Unity.Rpc.Models;
// using Solana.Unity.SDK;
// using Solana.Unity.SolanaMobileStack;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
//
// /// <summary>
// /// Exercises every Mobile Wallet Adapter v2 method against the active
// /// <see cref="SolanaWalletAdapter"/>. Each action is a parameterless public method,
// /// so you can wire it directly to a uGUI Button's OnClick() in the inspector.
// /// Optionally assign <see cref="statusText"/> to surface results on screen.
// ///
// /// Only meaningful on Android after logging in via the wallet adapter
// /// (Web3.Instance.LoginWalletAdapter()). For manual/dev testing.
// /// </summary>
// public class MwaV2Demo : MonoBehaviour
// {
//     [Tooltip("Optional. A uGUI Text to show the latest results (also logged to the console).")]
//     [SerializeField] private TextMeshProUGUI statusText;
//
//     [Tooltip("How many recent log lines to keep in the status text.")]
//     [SerializeField] private int maxLines = 16;
//
//     [Header("Buttons (assign in inspector - auto-wired in Start)")]
//     [SerializeField] private Button reconnectButton;
//     [SerializeField] private Button getCapabilitiesButton;
//     [SerializeField] private Button signMessageButton;
//     [SerializeField] private Button cloneAuthorizationButton;
//     [SerializeField] private Button loginWithSignInButton;
//     [SerializeField] private Button signAndSendButton;
//     [SerializeField] private Button disconnectButton;
//     [SerializeField] private Button deauthorizeButton;
//
//     private readonly List<string> _log = new();
//     private bool _busy;
//
//     private SolanaWalletAdapter Adapter => Web3.Instance?.WalletBase as SolanaWalletAdapter;
//
//     private void Start()
//     {
//         Wire(reconnectButton, DoReconnect);
//         Wire(getCapabilitiesButton, DoGetCapabilities);
//         Wire(signMessageButton, DoSignMessage);
//         Wire(cloneAuthorizationButton, DoCloneAuthorization);
//         Wire(loginWithSignInButton, DoLoginWithSignIn);
//         Wire(signAndSendButton, DoSignAndSend);
//         Wire(disconnectButton, DoDisconnect);
//         Wire(deauthorizeButton, DoDeauthorize);
//
//         // Optional methods stay disabled until GetCapabilities confirms the wallet
//         // supports them (Phantom, for example, has no clone_authorization).
//         SetInteractable(cloneAuthorizationButton, false);
//         SetInteractable(signAndSendButton, false);
//     }
//
//     private static void Wire(Button button, UnityEngine.Events.UnityAction action)
//     {
//         if (button != null) button.onClick.AddListener(action);
//     }
//
//     private static void SetInteractable(Button button, bool value)
//     {
//         if (button != null) button.interactable = value;
//     }
//
//     private void DoReconnect() => Run("Reconnect", async () =>
//     {
//         var r = await Adapter.Reconnect();
//         Log($"Reconnect -> {r.GetType().Name}");
//     });
//
//     private void DoGetCapabilities() => Run("GetCapabilities", async () =>
//     {
//         var c = await Adapter.GetCapabilities();
//         Log($"caps: maxTx={c?.MaxTransactionsPerRequest}, " +
//             $"signAndSend={c?.SupportsSignAndSendTransactions}, clone={c?.SupportsCloneAuthorization}");
//         // Raw feature identifiers the wallet advertised — so we can confirm the exact
//         // strings (e.g. solana:signAndSendTransaction) the OR-detection matches against.
//         Log($"caps features: [{(c?.Features != null ? string.Join(", ", c.Features) : "<none>")}]");
//
//         // Enable/disable the optional-method buttons based on what the wallet supports.
//         SetInteractable(signAndSendButton, c?.SupportsSignAndSendTransactions ?? false);
//         SetInteractable(cloneAuthorizationButton, c?.SupportsCloneAuthorization ?? false);
//     });
//
//     private void DoSignMessage() => Run("SignMessage", async () =>
//     {
//         var sig = await Adapter.SignMessage("Hello from Lumberjack (MWA v2)");
//         Log($"signature: {Convert.ToBase64String(sig)}");
//     });
//
//     private void DoCloneAuthorization() => Run("CloneAuthorization", async () =>
//     {
//         var token = await Adapter.CloneAuthorization();
//         Log($"cloned auth token: {Truncate(token)}");
//     });
//
//     private void DoLoginWithSignIn() => Run("LoginWithSignIn", async () =>
//     {
//         var payload = new SignInPayload { Domain = "lumberjack.game", Statement = "Sign in to Lumberjack" };
//         var (acc, siws) = await Adapter.LoginWithSignIn(payload);
//         Log($"SIWS ok: {acc.PublicKey} sig={Truncate(siws?.Signature)}");
//     });
//
//     private void DoSignAndSend() => Run("SignAndSendTransactions", async () =>
//     {
//         var tx = await BuildSelfTransfer(1);
//         var res = await Adapter.SignAndSendTransactions(new[] { tx });
//         if (res is SignAndSendTxResult.Success s) Log($"sent: {s.Signatures.Length} signature(s)");
//         else Log($"result: {res.GetType().Name}");
//     });
//
//     private void DoDisconnect() => Run("Disconnect", async () =>
//     {
//         await Adapter.Disconnect();
//         Log("disconnected locally - remembered wallet retained");
//     });
//
//     private void DoDeauthorize() => Run("Deauthorize", async () =>
//     {
//         var r = await Adapter.Deauthorize();
//         Log($"Deauthorize -> {r.GetType().Name}");
//     });
//
//     private async void Run(string label, Func<Task> action)
//     {
//         if (Adapter == null) { Log("No SolanaWalletAdapter - log in via the wallet adapter first."); return; }
//         if (_busy) { Log("busy - wait for the current operation"); return; }
//         _busy = true;
//         Log($"> {label} ...");
//         try { await action(); }
//         catch (Exception e) { Log($"x {label}: {e.GetType().Name}: {e.Message}"); }
//         finally { _busy = false; }
//     }
//
//     private void Log(string line)
//     {
//         _log.Insert(0, line);
//         if (_log.Count > Mathf.Max(1, maxLines)) _log.RemoveAt(_log.Count - 1);
//         if (statusText != null) statusText.text = string.Join("\n", _log);
//         Debug.Log($"[MWA v2 Demo] {line}");
//     }
//
//     private static string Truncate(string s) =>
//         string.IsNullOrEmpty(s) ? "<null>" : (s.Length <= 16 ? s : s.Substring(0, 16) + "...");
//
//     // A proper unsigned self-transfer for the wallet to sign + submit via
//     // sign_and_send_transactions. A valid transaction's wire format carries one signature
//     // slot per required signer; the fee payer is the only signer here, so we include one
//     // empty slot for the wallet to fill. (With zero slots the serialized tx contradicts
//     // the message header's numRequiredSignatures and the wallet rejects it as malformed.)
//     private static async Task<Transaction> BuildSelfTransfer(ulong lamports)
//     {
//         var me = Web3.Account.PublicKey;
//         return new Transaction
//         {
//             RecentBlockHash = await Web3.BlockHash(),
//             FeePayer = me,
//             Instructions = new List<TransactionInstruction> { SystemProgram.Transfer(me, me, lamports) },
//             Signatures = new List<SignaturePubKeyPair>
//             {
//                 new SignaturePubKeyPair { PublicKey = me, Signature = new byte[64] }
//             }
//         };
//     }
// }
