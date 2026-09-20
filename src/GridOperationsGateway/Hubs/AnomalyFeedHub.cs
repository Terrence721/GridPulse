using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GridPulse.GridOperationsGateway.Hubs;

// Pure server-to-client broadcast channel for this slice - dispatchers
// don't invoke hub methods, they just listen for "AnomalyDetected".
[Authorize]
public sealed class AnomalyFeedHub : Hub;
