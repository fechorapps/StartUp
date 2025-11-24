Diagrama de Contexto (Actualizado) - Service Management System
Descripción General
El sistema es una capa de IA intermedia que permite a huéspedes solicitar servicios de mantenimiento mediante conversación natural. El sistema interpreta las solicitudes, las transforma en work orders estructuradas y las envía a plataformas de gestión de propiedades (Buildium, AppFolio, Hostify), las cuales se encargan de la comunicación directa con los contratistas.

Actores (Personas)
ActorDescripciónInteracción con el SistemaGuestUsuario que renta una propiedad y necesita solicitar serviciosEnvía mensajes al chatbot IA, sube fotos, recibe actualizaciones de estadoProperty OwnerDueño de las propiedadesConfigura propiedades, conecta plataformas externas, define reglas de negocioContractorProfesional de servicios⚠️ NO interactúa directamente con el sistema - Recibe trabajo vía Buildium/AppFolio/Hostify

Arquitectura de Integración
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           FLUJO DE COMUNICACIÓN                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

                    ┌─────────────┐
                    │    Guest    │
                    │    (App)    │
                    └──────┬──────┘
                           │
                           │ Chat / Solicitudes
                           │ [HTTPS/WSS]
                           ▼
              ┌────────────────────────────┐
              │                            │
              │   Service Management       │
              │        System              │
              │                            │
              │   • AI Assistant Layer     │
              │   • Intent Recognition     │
              │   • Work Order Creation    │
              │   • Status Tracking        │
              │                            │
              └─────────────┬──────────────┘
                            │
          ┌─────────────────┼─────────────────┐
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────┐   ┌─────────────┐   ┌─────────────┐
│  Buildium   │   │  AppFolio   │   │   Hostify   │
│             │   │             │   │             │
│ • Vendors   │   │ • Vendors   │   │ • Vendors   │
│ • WorkOrders│   │ • WorkOrders│   │ • Tasks     │
│ • Webhooks  │   │ • Webhooks  │   │ • Webhooks  │
└──────┬──────┘   └──────┬──────┘   └──────┬──────┘
│                 │                 │
│    Notifica     │    Notifica     │    Notifica
│    y asigna     │    y asigna     │    y asigna
▼                 ▼                 ▼
┌─────────────────────────────────────────────────┐
│                  CONTRACTORS                     │
│                                                  │
│   • Reciben trabajo desde su plataforma         │
│   • Actualizan estado en su plataforma          │
│   • NO conocen nuestro sistema directamente     │
│                                                  │
└─────────────────────────────────────────────────┘

Flujo de Datos Detallado
1. Solicitud del Guest (Outbound)
   Guest App ──> Sistema ──> Claude API (análisis)
   │
   └──> Buildium/AppFolio/Hostify
   │
   └──> Crea Work Order
   └──> Asigna Vendor automáticamente (según reglas de la plataforma)
   └──> Plataforma notifica al Contractor
2. Actualizaciones de Estado (Inbound via Webhooks)
   Contractor actualiza estado en Buildium
   │
   ▼
   Buildium envía Webhook ──> Sistema
   │
   ├──> Actualiza ServiceRequest.Status
   ├──> Notifica al Guest via Push/WebSocket
   └──> Guarda historial de cambios

Sistemas Externos (Actualizado)
SistemaRolComunicaciónBuildiumPMS - Gestiona vendors y work ordersREST API (outbound) + Webhooks (inbound)AppFolioPMS - Gestiona contractors y work ordersREST API (outbound) + Webhooks (inbound)HostifyPMS - Short-term rentals, tasksREST API (outbound) + Webhooks (inbound)Claude APIIA - Procesa lenguaje naturalREST API (outbound only)

Webhooks que el Sistema Recibe
PlataformaEventoAcción en el SistemaBuildiumworkorder.assignedActualiza ServiceRequest.Status = AssignedBuildiumworkorder.startedActualiza ServiceRequest.Status = InProgressBuildiumworkorder.completedActualiza ServiceRequest.Status = Completed, solicita ratingBuildiumworkorder.cancelledActualiza ServiceRequest.Status = Cancelled, notifica GuestAppFoliowork_order.status_changedMapea estado y actualiza ServiceRequestHostifytask.updatedMapea estado y actualiza ServiceRequest

Ejemplo de Flujo Completo (Actualizado)
TIEMPO    ACTOR/SISTEMA         ACCIÓN
──────    ─────────────         ──────
T+0       Guest                 Escribe: "Tengo una fuga en el baño"
T+1       Sistema → Claude      Analiza intent → Plumbing, High Priority
T+2       Sistema → Guest       "¿Puedes enviar una foto?"
T+3       Guest                 Envía foto
T+4       Sistema → Claude      Analiza imagen → Confirma fuga activa
T+5       Sistema → Buildium    POST /workorders { type: "Plumbing", priority: "High", ... }
T+6       Buildium              Asigna automáticamente a vendor según reglas configuradas
T+7       Buildium              Notifica al contractor via su app/email
T+8       Buildium → Sistema    Webhook: workorder.assigned { vendorName: "Juan Plomero", scheduledFor: "3pm" }
T+9       Sistema → Guest       "Se asignó a Juan Plomero, llegará a las 3pm"
T+10      Contractor            Acepta trabajo en app de Buildium
T+11      Buildium → Sistema    Webhook: workorder.confirmed
T+12      Sistema → Guest       "El contratista confirmó la visita"
T+13      Contractor            Llega y marca "In Progress" en Buildium
T+14      Buildium → Sistema    Webhook: workorder.started
T+15      Sistema → Guest       "El contratista ha llegado y está trabajando"
T+16      Contractor            Termina y marca "Completed" en Buildium
T+17      Buildium → Sistema    Webhook: workorder.completed { notes: "Replaced pipe", cost: 150 }
T+18      Sistema → Guest       "¡Trabajo completado! ¿Cómo calificarías el servicio?"