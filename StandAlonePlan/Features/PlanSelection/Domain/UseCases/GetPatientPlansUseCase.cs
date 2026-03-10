using System;
using System.Collections.Generic;
using System.Linq;
using StandAlonePlan.Features.PlanSelection.Data;
using StandAlonePlan.Features.PlanSelection.Domain.Models;

namespace StandAlonePlan.Features.PlanSelection.Domain.UseCases
{
    /// <summary>
    /// Loads and filters all plans available for a patient.
    /// Direct port of SETPLAN.CBL LOAD-PLAN-ARRAY logic.
    /// Max 27 plans (PLAN-ARRAY-MAX) mirrored as MaxPlans constant.
    /// </summary>
    public class GetPatientPlansUseCase
    {
        private const int MaxPlans = 27; // PLAN-ARRAY-MAX VALUE 27
        private readonly IPlanRepository _repository;

        public GetPatientPlansUseCase(IPlanRepository repository) => _repository = repository;

        public IReadOnlyList<Plan> Execute(int patientNumber, PlanSelectionMode mode)
        {
            var plans      = new List<Plan>(MaxPlans);
            bool cashOff   = !_repository.IsCashDisabled();   // 88 R1A-DISABLE-CASH-OFF
            bool showDelAdd = mode == PlanSelectionMode.ShowDeleteAdd;
            var  today     = DateTime.Today;

            // ── Primary plans — VARYING N1 FROM 1 BY 1 UNTIL N1 > 3 ──────────
            // R5-PP(1), R5-PP(2), R5-PP(3)
            var primaryCodes = _repository.GetPrimaryPlanCodes(patientNumber);

            foreach (var rawCode in primaryCodes.Take(3))
            {
                if (string.IsNullOrWhiteSpace(rawCode)) continue;
                var code = rawCode.Trim();

                // R1A-DISABLE-CASH-OFF OR R5-PP(N1) NOT = 'C'
                if (!cashOff && code == "C") continue;

                var master = _repository.GetPlanMaster(code);
                if (master == null) continue;

                // STATUS-OK AND (SHOW-DELETE-ADD OR NOT R11-DELETED)
                if (master.IsDeleted && !showDelAdd) continue;

                var pr = _repository.GetPatientPlanRecord(patientNumber, code);

                // (NOT R18-DELETED AND (R18-EXP-DATE=ZERO OR R18-EXP-DATE>=SYS-DATE))
                //   OR R5-PP(N1)='C' OR SHOW-DELETE-ADD
                bool isCash        = code == "C";
                bool activeInR18   = pr != null && !pr.IsDeleted &&
                                     (!pr.ExpirationDate.HasValue || pr.ExpirationDate.Value >= today);
                if (!activeInR18 && !isCash && !showDelAdd) continue;

                plans.Add(BuildPlan(code, master, pr, today));
                if (plans.Count >= MaxPlans) break;
            }

            // ── Additional plans from R18FILE sequential ──────────────────────
            var primarySet = new HashSet<string>(primaryCodes.Select(c => c.Trim()),
                                                 StringComparer.OrdinalIgnoreCase);

            foreach (var pr in _repository.GetAllPatientPlanRecords(patientNumber))
            {
                if (plans.Count >= MaxPlans) break;

                var code = pr.PlanCode.Trim();

                // Skip duplicates of primary plans
                if (primarySet.Contains(code)) continue;
                if (!cashOff && code == "C") continue;

                bool active = !pr.IsDeleted &&
                              (!pr.ExpirationDate.HasValue || pr.ExpirationDate.Value >= today);
                if (!active && !showDelAdd) continue;

                if (pr.IsDeleted)
                {
                    plans.Add(new Plan { PlanCode = code, Name = code, IsDeleted = true });
                    continue;
                }

                if (pr.ExpirationDate.HasValue && pr.ExpirationDate.Value < today)
                {
                    plans.Add(new Plan { PlanCode = code, Name = code,
                                         ExpirationDate = pr.ExpirationDate });
                    continue;
                }

                var master = _repository.GetPlanMaster(code);
                if (master == null) continue;

                if (master.IsDeleted)
                {
                    if (showDelAdd)
                        plans.Add(new Plan { PlanCode = code, Name = master.Name, IsDeleted = true });
                    continue;
                }

                plans.Add(new Plan { PlanCode = code, Name = " " + master.Name });
            }

            return plans.AsReadOnly();
        }

        // Builds a Plan entity merging R11FILE master + R18FILE patient record.
        private static Plan BuildPlan(string code, PlanMasterRecord master,
                                      PatientPlanRecord? pr, DateTime today)
        {
            bool deleted = pr?.IsDeleted ?? master.IsDeleted;
            var  expDate = pr?.ExpirationDate;
            string name  = deleted                                          ? master.Name
                         : expDate.HasValue && expDate.Value < today        ? master.Name
                         : " " + master.Name;

            return new Plan
            {
                PlanCode       = code,
                Name           = name,
                IsDeleted      = deleted,
                ExpirationDate = expDate
            };
        }
    }
}
