# Production Readiness Checklist

> **Gate obligatorio antes de deploy a producción**
> Última actualización: 2026-04-08

---

## 🔴 P0 - BLOQUEANTES (No deploy sin esto)

### Secret Management

- [ ] **Google Secret Manager configurado**
  - [ ] Todos los secrets migrados de env vars a Secret Manager
  - [ ] IAM roles configurados (principle of least privilege)
  - [ ] Rotación automática habilitada para:
  - [ ] JWT_SECRET (cada 90 días)
    - [ ] STRIPE_SECRET_KEY (manual, con proceso documentado)
    - [ ] DATABASE credentials
- [ ] **Auditoría de acceso habilitada** (Cloud Audit Logs)
- [ ] **No hay secrets en código ni en CI logs**

### Database

- [ ] **Backups automáticos configurados**
  - [ ] Cloud SQL: backups diarios habilitados
  - [ ] Retención: mínimo 30 días
  - [ ] Point-in-time recovery habilitado
- [ ] **Restore probado** (fecha del último test: **\_\_\_**)
- [ ] **Conexiones SSL obligatorias**

### ML Service

- [ ] **Fallback implementado**
  - [ ] Si ML falla → usar reglas de negocio default
  - [ ] Fallback probado en staging
- [ ] **Timeout configurado** (máximo 5s para scoring)
- [ ] **Circuit breaker activo** (Polly policy hacia ML)
- [ ] **Health check de ML en /health**
- [ ] **Versionado de modelo**
  - [ ] Modelo actual documentado: v**\_**
  - [ ] Proceso de rollback documentado

### Observabilidad

- [ ] **Exportadores configurados**
  - [ ] Traces → Cloud Trace / Grafana Tempo
  - [ ] Metrics → Cloud Monitoring / Prometheus
  - [ ] Logs → Cloud Logging / Loki
- [ ] **Dashboards creados**
  - [ ] Latencia por endpoint (p50, p95, p99)
  - [ ] Error rate por módulo
  - [ ] ML latency específico
  - [ ] Queue backlog (Outbox, Hangfire)
- [ ] **Alertas configuradas**
  - [ ] 5xx rate > 1% por 5 min
  - [ ] Latencia p99 > 2s
  - [ ] Stripe failures > 3 en 5 min
  - [ ] ML timeout rate > 5%
  - [ ] Outbox backlog > 100 items
  - [ ] Disk usage > 80%

---

## 🟡 P1 - CRÍTICOS (Deploy con riesgo aceptado)

### Seguridad

- [ ] **Penetration test realizado** (fecha: **\_\_\_**)
- [ ] **OWASP Top 10 revisado**
- [ ] **Rate limiting validado** en endpoints críticos:
  - [ ] /auth/login (5 req/min por IP)
  - [ ] /auth/signup (3 req/min por IP)
  - [ ] /payments/\* (10 req/min por tenant)
  - [ ] /webhooks/\* (100 req/min global)

### Resiliencia

- [ ] **Circuit breakers validados**
  - [ ] Stripe: open después de 5 failures
  - [ ] ML: open después de 3 timeouts
  - [ ] ClamAV: degraded mode funciona
- [ ] **Retry policies configuradas**
  - [ ] Stripe: 3 retries con exponential backoff
  - [ ] Email: 3 retries
  - [ ] Webhooks outbound: 5 retries

### Performance

- [ ] **Load test ejecutado**
  - [ ] Target: **\_** RPS
  - [ ] Resultado: **\_** RPS sostenido
  - [ ] Latencia p99 bajo carga: **\_** ms
- [ ] **Database indexes validados**
- [ ] **Connection pooling configurado**

---

## 🟢 P2 - RECOMENDADOS (Post-launch)

### Operaciones

- [ ] **Runbooks documentados**
  - [ ] Stripe caído
  - [ ] Redis caído
  - [ ] ML devuelve errores
  - [ ] Database failover
  - [ ] Rollback de deploy
- [ ] **On-call rotation definida**
- [ ] **Incident response process documentado**

### Compliance

- [ ] **Data retention policy definida**
- [ ] **PII handling documentado**
- [ ] **Audit trail completo** (quién hizo qué, cuándo)

### Disaster Recovery

- [ ] **RTO definido**: **\_** minutos
- [ ] **RPO definido**: **\_** minutos
- [ ] **DR drill ejecutado** (fecha: **\_\_\_**)

---

## Firmas de Aprobación

| Rol       | Nombre | Fecha | ✓   |
| --------- | ------ | ----- | --- |
| Tech Lead |        |       |     |
| Security  |        |       |     |
| Ops/SRE   |        |       |     |

---

## Notas

```markdown
Ambiente: Production
Región:
Versión:
Commit:
```
