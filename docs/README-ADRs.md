# Architecture Decision Records (ADRs)

Este diretório contém as decisões arquiteturais oficiais do projeto Desafio com IA.

---

## 📋 Índice de ADRs

### ADR-002: Padrão Arquitetural para APIs RESTful
**Arquivo:** `tasks\002\tasks-002-arch-decision.md`  
**Status:** ✅ Aceito  
**Data:** 16/01/2026  
**Autor:** Kallebe Lins

**Resumo:** Estabelece os padrões obrigatórios para implementação de todos os endpoints e serviços da API, incluindo:
- Estrutura de rotas RESTful
- Padrões CQRS com Mvp24Hours
- Status codes HTTP
- DTOs e validações
- Tratamento de erros com ProblemDetails
- Documentação Swagger/OpenAPI
- Paginação com PagedResult

**Módulo de Referência:** Clientes (implementação completa conforme ADR)

**Aplicável a:** Todos os futuros módulos e recursos da API

---

## 🎯 Como Usar Este Diretório

### Para Implementar Novo Módulo
1. Leia `tasks\002\tasks-002-arch-decision.md` completamente
2. Use o checklist de implementação fornecido
3. Consulte o módulo Clientes como referência
4. Valide conformidade antes de PR

### Para Propor Mudança Arquitetural
1. Crie novo arquivo `tasks-{backlog-id}-arch-decision.md`
2. Use template de ADR (estrutura similar ao ADR-002)
3. Documente contexto, decisão e consequências
4. Obtenha aprovação antes de implementar

### Para Revisar Código
1. Valide conformidade com ADR-002
2. Aponte desvios não documentados
3. Solicite justificativa para exceções

---

## 📝 Template de ADR

```markdown
# ADR-XXX: Título da Decisão

**Status:** [Proposto | Aceito | Rejeitado | Obsoleto]  
**Data da Decisão:** DD/MM/AAAA  
**Contexto:** Breve descrição do contexto  
**Autor:** Nome do Responsável

## Contexto e Problema

Descrição do problema ou necessidade que motivou a decisão.

## Decisão

Descrição clara da decisão tomada.

## Consequências

### Positivas
- Benefício 1
- Benefício 2

### Negativas
- Trade-off 1
- Trade-off 2

## Alternativas Consideradas

1. **Alternativa 1:** Descrição + razão para não adotar
2. **Alternativa 2:** Descrição + razão para não adotar

## Referências

- Link 1
- Link 2
```

---

## 📚 Convenções

- **Numeração:** ADR-XXX (3 dígitos, ex: ADR-001, ADR-002)
- **Nomenclatura:** `tasks-002-arch-decision.md`
- **Status:** Proposto → Aceito/Rejeitado → Obsoleto (se necessário)
- **Versionamento:** Git rastreia histórico de mudanças

---

**Última atualização:** 16/01/2026  
**Responsável:** Kallebe Lins
