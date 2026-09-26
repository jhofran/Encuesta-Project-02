# Documento de Visión — Módulo Encuesta

| Campo   | Valor                    |
|---------|--------------------------|
| Versión | 1.0 (borrador)           |
| Fecha   | 2026-09-26               |
| Estado  | Pendiente de validación  |

## 1. Propósito

El módulo **Encuesta** permite crear, publicar, responder y analizar encuestas de manera simple y confiable, de forma que las organizaciones obtengan retroalimentación estructurada de sus públicos sin depender de herramientas externas.

## 2. Problema y oportunidad

| Problema actual | Consecuencia |
|-----------------|--------------|
| La recolección de opiniones se hace con herramientas dispersas (hojas de cálculo, correos, formularios ad hoc) | Datos inconsistentes y difíciles de consolidar |
| No hay control sobre respuestas duplicadas ni sobre la vigencia de la encuesta | Resultados sesgados o inválidos |
| Los resultados se procesan manualmente | Demora en la toma de decisiones |

**Oportunidad:** un módulo único que cubra el ciclo completo (diseño → publicación → respuesta → resultados) con reglas de negocio claras.

## 3. Objetivos

### 3.1 Objetivos de negocio
| ID | Objetivo | Indicador de éxito |
|----|----------|--------------------|
| OB-01 | Reducir el tiempo de creación de una encuesta | Una encuesta de 10 preguntas se crea y publica en < 10 min |
| OB-02 | Aumentar la tasa de respuesta | ≥ 60 % de los invitados completan la encuesta |
| OB-03 | Garantizar la integridad de los datos | 0 respuestas duplicadas por participante en encuestas de respuesta única |
| OB-04 | Acelerar el análisis | Resultados agregados disponibles en tiempo real (< 5 s tras enviar una respuesta) |

### 3.2 Objetivos del sistema
- Permitir definir encuestas con preguntas de distintos tipos.
- Controlar el ciclo de vida: `Borrador → Publicada → Cerrada`.
- Registrar respuestas de forma validada y auditable.
- Mostrar resultados agregados y permitir su exportación.

## 4. Alcance

### 4.1 Dentro del alcance (MVP)
- Creación y edición de encuestas en borrador.
- Tipos de pregunta: opción única, opción múltiple, texto libre y escala (1–5).
- Publicación mediante enlace único; cierre manual o por fecha límite.
- Respuesta de encuestas (anónima o identificada según configuración).
- Resultados agregados por pregunta y exportación a CSV.

### 4.2 Fuera del alcance (MVP)
- Lógica condicional entre preguntas (saltos).
- Envío masivo de invitaciones por correo.
- Análisis estadístico avanzado y segmentación.
- Internacionalización más allá del español.

## 5. Perfiles de usuario (Stakeholders)

| Perfil | Descripción | Necesidades principales | Nivel técnico |
|--------|-------------|-------------------------|---------------|
| **Administrador** | Gestiona el sistema y supervisa todas las encuestas | Auditar, cerrar o eliminar cualquier encuesta; gestionar usuarios | Alto |
| **Creador de encuestas** | Diseña, publica y analiza encuestas | Crear rápido, controlar vigencia, ver y exportar resultados | Medio |
| **Participante** | Persona que responde una encuesta mediante un enlace | Responder de forma sencilla, sin fricción, desde cualquier dispositivo | Bajo |
| **Analista** (lectura) | Consulta resultados sin modificar encuestas | Acceder a resultados agregados y exportarlos | Medio |

## 6. Requisitos funcionales de alto nivel

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| RF-01 | El creador puede crear una encuesta con título, descripción y ≥ 1 pregunta | Alta |
| RF-02 | El creador puede editar una encuesta solo mientras está en borrador | Alta |
| RF-03 | El creador puede publicar una encuesta y obtener un enlace único | Alta |
| RF-04 | El participante puede responder una encuesta publicada y vigente | Alta |
| RF-05 | El sistema impide más de una respuesta por participante si la encuesta es de respuesta única | Alta |
| RF-06 | El creador puede cerrar una encuesta manualmente; el sistema la cierra al vencer la fecha límite | Alta |
| RF-07 | El creador/analista puede ver resultados agregados por pregunta | Alta |
| RF-08 | El creador/analista puede exportar resultados a CSV | Media |
| RF-09 | El administrador puede eliminar encuestas | Media |

## 7. Reglas de negocio

| ID | Regla |
|----|-------|
| RN-01 | Una encuesta debe tener al menos una pregunta para poder publicarse. |
| RN-02 | Una encuesta publicada no puede modificar sus preguntas. |
| RN-03 | Solo se aceptan respuestas mientras la encuesta esté `Publicada` y dentro de su fecha límite. |
| RN-04 | Las preguntas marcadas como obligatorias deben responderse para enviar. |
| RN-05 | Las opciones de una pregunta de opción única/múltiple deben ser ≥ 2. |
| RN-06 | En encuestas anónimas no se almacena información que identifique al participante. |
| RN-07 | Una encuesta cerrada no puede reabrirse (se debe duplicar). |

## 8. Supuestos, restricciones y riesgos

**Supuestos**
- Los creadores están autenticados; los participantes pueden responder sin cuenta.
- El acceso es vía navegador web (responsive).

**Restricciones**
- Los datos personales se tratan conforme a la normativa de protección de datos aplicable.

**Riesgos**
| Riesgo | Mitigación |
|--------|------------|
| Respuestas duplicadas en encuestas anónimas | Control por token de sesión/cookie de un solo uso |
| Baja participación | Interfaz mínima y barra de progreso |
| Abuso del enlace público (bots) | Límite de tasa y validación en servidor |

## 9. Criterios de éxito del MVP
1. Un creador completa el ciclo crear → publicar → ver resultados sin asistencia.
2. Un participante responde en móvil en menos de 3 min para una encuesta de 10 preguntas.
3. Todas las reglas RN-01 a RN-07 están cubiertas por escenarios de aceptación (ver [02-user-stories.md](02-user-stories.md)).

## 10. Trazabilidad

| Historia (02-user-stories.md) | Requisitos | Diagrama |
|-------------------------------|------------|----------|
| HU-01 Crear encuesta | RF-01, RF-02, RN-01, RN-05 | [diagrams/01-crear-encuesta.svg](diagrams/01-crear-encuesta.svg) |
| HU-02 Publicar encuesta | RF-03, RN-01, RN-02 | [diagrams/02-publicar-encuesta.svg](diagrams/02-publicar-encuesta.svg) |
| HU-03 Responder encuesta | RF-04, RF-05, RN-03, RN-04, RN-06 | [diagrams/03-responder-encuesta.svg](diagrams/03-responder-encuesta.svg) |
| HU-04 Cerrar encuesta | RF-06, RN-07 | [diagrams/04-cerrar-encuesta.svg](diagrams/04-cerrar-encuesta.svg) |
| HU-05 Ver resultados | RF-07, RF-08 | [diagrams/05-ver-resultados.svg](diagrams/05-ver-resultados.svg) |
