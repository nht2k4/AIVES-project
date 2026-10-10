# TV5 Ass2 implementation plan

Goal: implement Ass2 tasks 5.1, 5.2 and 5.3 against the assignment's existing contracts in `../AIVES_CongViec_TV5.md` and `../AIVES_TV5_DoiChieu_ProjectMau.md`.

## Constraints and completion criteria

- Preserve shared interfaces, DTOs, entities, models and contract tests. The user explicitly deferred the editing mini-task until Tien approves the interface addition.
- Do not implement TV2/TV3/TV4/TV6 files to conceal missing dependencies.
- Web pages call Business interfaces only. Subject access is checked by services on GET and POST; admin/lecturer route restrictions are declared on the pages.
- POST forms use antiforgery, successful writes redirect with TempData messages, validation retains input, missing records return 404 and forbidden requests go to AccessDenied.
- Build TV5 pages with no new compiler warnings; run original contract tests when their packages are available. Record failures and distinguish isolated verification with test doubles from actual integration.
- Verify rendered pages, links, role restrictions, invalid forms, mutation flows and antiforgery through an isolated verification host. Actual SQL/application verification remains dependent on other members.

## Task 1 — questions

- [x] Run the existing question tests before implementation and record the failure.
- [x] Implement `QuestionAllocator.cs`: rank by previous-set exclusion, usage count and caller RNG. Reject invalid arguments and insufficient distinct pool without silently returning short sets.
- [x] Implement `QuestionService.cs`: check permission, validate/trim text before mutation, include inactive questions, resolve question subject before toggling.
- [x] Verify contract scenarios with real in-memory repositories and an explicitly named permission double if TV3 is unfinished. Add tests for role rejection, length boundaries, invalid allocation and reproducibility.

## Task 2 — subject/language pages

- [x] Implement `Pages/Subjects/Index`, `Details`, `Pages/MySubjects/Index`, `Pages/Language/Edit` and a narrowly scoped TV5 helper/layout that does not replace TV6's foundation.
- [x] Check admin-only subject routes, lecturer-only MySubjects, service access on language GET/POST, retained form input, notices and 404s.
- [x] Use subject DTO lecturer options directly; exclude filtering/business rules from the web layer.

## Task 3 — question bank

- [x] Implement `Pages/Exams/Questions`: active/inactive table, content/points add form, toggle handler; links from subject pages.
- [x] Check the actual question's subject on toggle so a forged query cannot modify an unrelated question through another subject's page.
- [x] Verify POST antiforgery, forbidden access, invalid input, redirects, preserved data and rendered escaping.

## Handoff

- [x] Record test commands/results, allocation complexity and hand examples (5/3/2 and 8/4/3).
- [x] Record TV2 repository, TV3 services/permission/exam integration, TV4 login/AccessDenied and TV6 host/layout dependency gaps.
- [x] Review diffs and build; do not claim official tests, SQL/browser integration, review, merge or push without evidence.


## Outstanding acceptance gates

- [ ] Full application acceptance remains pending TV2/TV3/TV4/TV6 dependencies and a configured SQL test database.
- [ ] Question-editing mini-task remains pending Tien agreement (explicitly deferred by the user).
- [ ] TV3 team cross-review, integrated checklist, PR approval and merge.
