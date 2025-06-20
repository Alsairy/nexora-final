using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexora.Core.DTOs;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/esignature/[controller]")]
    [Authorize]
    public class WorkflowController : ControllerBase
    {
        private readonly IESignatureWorkflowService _workflowService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<WorkflowController> _logger;

        public WorkflowController(
            IESignatureWorkflowService workflowService,
            ICurrentUserService currentUserService,
            ILogger<WorkflowController> logger)
        {
            _workflowService = workflowService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CreateWorkflow([FromBody] CreateESignatureWorkflowRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var workflow = await _workflowService.CreateWorkflowAsync(request, tenantId, userId);
                
                _logger.LogInformation("Workflow {WorkflowId} created successfully by user {UserId}", workflow.Id, userId);
                
                return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for creating workflow: {Message}", ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workflow");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetWorkflow(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var workflow = await _workflowService.GetWorkflowAsync(id, tenantId);

                if (workflow == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                return Ok(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ESignatureWorkflowListResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetWorkflows([FromQuery] ESignatureWorkflowListRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var workflows = await _workflowService.GetWorkflowsAsync(request, tenantId);

                return Ok(workflows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflows");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> UpdateWorkflow(int id, [FromBody] UpdateESignatureWorkflowRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var workflow = await _workflowService.UpdateWorkflowAsync(id, request, tenantId, userId);
                
                if (workflow == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                _logger.LogInformation("Workflow {WorkflowId} updated successfully by user {UserId}", id, userId);
                
                return Ok(workflow);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for updating workflow {WorkflowId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> DeleteWorkflow(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var result = await _workflowService.DeleteWorkflowAsync(id, tenantId, userId);
                
                if (!result)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                _logger.LogInformation("Workflow {WorkflowId} deleted successfully by user {UserId}", id, userId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{workflowId}/steps")]
        [ProducesResponseType(typeof(ESignatureWorkflowStepResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> AddWorkflowStep(int workflowId, [FromBody] CreateESignatureWorkflowStepRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var step = await _workflowService.AddWorkflowStepAsync(workflowId, request, tenantId, userId);
                
                _logger.LogInformation("Step {StepId} added to workflow {WorkflowId} by user {UserId}", step.Id, workflowId, userId);
                
                return CreatedAtAction(nameof(GetWorkflowStep), new { stepId = step.Id }, step);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for adding step to workflow {WorkflowId}: {Message}", workflowId, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding step to workflow {WorkflowId}", workflowId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("steps/{stepId}")]
        [ProducesResponseType(typeof(ESignatureWorkflowStepResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> GetWorkflowStep(int stepId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var step = await _workflowService.GetWorkflowStepAsync(stepId, tenantId);

                if (step == null)
                    return NotFound(new ErrorResponse { Message = "Workflow step not found" });

                return Ok(step);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow step {StepId}", stepId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{workflowId}/steps")]
        [ProducesResponseType(typeof(List<ESignatureWorkflowStepResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetWorkflowSteps(int workflowId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var steps = await _workflowService.GetWorkflowStepsAsync(workflowId, tenantId);

                return Ok(steps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting steps for workflow {WorkflowId}", workflowId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("steps/{stepId}/execute")]
        [ProducesResponseType(typeof(WorkflowExecutionResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ExecuteWorkflowStep(int stepId, [FromBody] ExecuteWorkflowStepRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.StepId = stepId;
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var result = await _workflowService.ExecuteWorkflowStepAsync(request, tenantId, userId);
                
                _logger.LogInformation("Workflow step {StepId} executed successfully by user {UserId}", stepId, userId);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for executing workflow step {StepId}: {Message}", stepId, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access for executing workflow step {StepId}: {Message}", stepId, ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow step {StepId}", stepId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> ActivateWorkflow(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var workflow = await _workflowService.ActivateWorkflowAsync(id, tenantId, userId);
                
                if (workflow == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                _logger.LogInformation("Workflow {WorkflowId} activated by user {UserId}", id, userId);
                
                return Ok(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/deactivate")]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> DeactivateWorkflow(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var workflow = await _workflowService.DeactivateWorkflowAsync(id, tenantId, userId);
                
                if (workflow == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                _logger.LogInformation("Workflow {WorkflowId} deactivated by user {UserId}", id, userId);
                
                return Ok(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/set-default")]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> SetDefaultWorkflow(int id)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var workflow = await _workflowService.SetDefaultWorkflowAsync(id, tenantId, userId);
                
                if (workflow == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                _logger.LogInformation("Workflow {WorkflowId} set as default by user {UserId}", id, userId);
                
                return Ok(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting workflow {WorkflowId} as default", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("{id}/clone")]
        [ProducesResponseType(typeof(ESignatureWorkflowResponse), 201)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        public async Task<IActionResult> CloneWorkflow(int id, [FromBody] CloneWorkflowRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());

                var workflow = await _workflowService.CloneWorkflowAsync(id, request.NewName, tenantId, userId);
                
                if (workflow == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                _logger.LogInformation("Workflow {WorkflowId} cloned as {NewWorkflowId} by user {UserId}", id, workflow.Id, userId);
                
                return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid request for cloning workflow {WorkflowId}: {Message}", id, ex.Message);
                return BadRequest(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("instances/document/{documentId}")]
        [ProducesResponseType(typeof(WorkflowInstanceResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetWorkflowInstance(int documentId)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var instance = await _workflowService.GetWorkflowInstanceAsync(documentId, tenantId);

                if (instance == null)
                    return NotFound(new ErrorResponse { Message = "Workflow instance not found" });

                return Ok(instance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow instance for document {DocumentId}", documentId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("instances/active")]
        [ProducesResponseType(typeof(List<WorkflowInstanceResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetActiveWorkflowInstances()
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var instances = await _workflowService.GetActiveWorkflowInstancesAsync(tenantId);

                return Ok(instances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active workflow instances");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("steps/pending")]
        [ProducesResponseType(typeof(List<WorkflowStepInstanceResponse>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetUserPendingSteps()
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var userId = int.Parse(_currentUserService.GetUserId());
                var pendingSteps = await _workflowService.GetUserPendingStepsAsync(userId, tenantId);

                return Ok(pendingSteps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending steps for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}/metrics")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetWorkflowMetrics(int id, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var metrics = await _workflowService.GetWorkflowMetricsAsync(id, tenantId, fromDate, toDate);

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metrics for workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpGet("{id}/usage-stats")]
        [ProducesResponseType(typeof(WorkflowUsageStatsResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> GetWorkflowUsageStats(int id, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var stats = await _workflowService.GetWorkflowUsageStatsAsync(tenantId, fromDate, toDate);

                if (stats == null)
                    return NotFound(new ErrorResponse { Message = "Workflow not found" });

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage stats for workflow {WorkflowId}", id);
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(WorkflowValidationResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        public async Task<IActionResult> ValidateWorkflowDefinition([FromBody] CreateESignatureWorkflowRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var tenantId = int.Parse(_currentUserService.GetTenantId());
                var validationResult = await _workflowService.ValidateWorkflowDefinitionAsync(request, tenantId);

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating workflow definition");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }
    }

    public class CloneWorkflowRequest
    {
        public string NewName { get; set; }
    }

    public class WorkflowExecutionResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string NextStepId { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    public class WorkflowInstanceResponse
    {
        public int Id { get; set; }
        public int WorkflowId { get; set; }
        public int DocumentId { get; set; }
        public string Status { get; set; }
        public string CurrentStepId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<WorkflowStepInstanceResponse> Steps { get; set; }
    }

    public class WorkflowStepInstanceResponse
    {
        public int Id { get; set; }
        public int StepId { get; set; }
        public string StepName { get; set; }
        public string Status { get; set; }
        public int? AssignedUserId { get; set; }
        public string AssignedUserName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Comments { get; set; }
    }

    public class WorkflowUsageStatsResponse
    {
        public int TotalExecutions { get; set; }
        public int CompletedExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public decimal CompletionRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public List<WorkflowStepStats> StepStats { get; set; }
    }

    public class WorkflowStepStats
    {
        public string StepName { get; set; }
        public int ExecutionCount { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public decimal SuccessRate { get; set; }
    }

    public class WorkflowValidationResponse
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
    }
}
