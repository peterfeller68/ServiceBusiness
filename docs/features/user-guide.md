# User Guide

Status: Implemented
Owner: Product
Last reviewed: 2026-08-04

## Problem

Users need in-app access to the user guides that are created and maintained as part of the development cycle.

## Personas

- Public visitor
- Pending user
- System Administrator
- Business Owner
- Business Employee
- Business Client
- Independent Home Owner

## Requirements

- The Help menu appears for unauthenticated, pending, and authenticated users.
- The Help menu includes a Getting Started item.
- The Help menu includes a User Guide item.
- Getting Started displays the contents of `docs/user-guide/getting-started.md`.
- User Guide displays the contents of `docs/user-guide/index.md`.
- Links in the user-guide index open the linked guide content in the application.
- `docs/user-guide/index.md` lists all user-guide files under `docs/user-guide/`, except `getting-started.md` and `index.md`.

## User Flows

### Open Getting Started

1. The user opens the Help menu.
2. The user chooses Getting Started.
3. The system displays the Getting Started guide.

### Open User Guide Index

1. The user opens the Help menu.
2. The user chooses User Guide.
3. The system displays the user-guide index.

### Open A Linked Guide

1. The user opens User Guide.
2. The user selects a guide link.
3. The system displays the selected guide content.

## UI Expectations

- Help menu items use the existing nested navigation style.
- Guide pages use the existing Help route family.
- Rendered guides use readable content styling inside the normal application shell.
- Markdown links to other guide files route to in-app Help pages.

## Data Model Impact

- None. User guides are static markdown files.

## Authorization Rules

- Help content is available to unauthenticated, pending, and authenticated users.
- Help content is not role-filtered in the current implementation.

## Acceptance Criteria

- Implemented: The Help menu item has a Getting Started menu item.
- Implemented: The Help menu item has a User Guide menu item.
- Implemented: Choosing Getting Started displays the contents of `docs/user-guide/getting-started.md`.
- Implemented: Choosing User Guide displays `docs/user-guide/index.md`.
- Implemented: Clicking a markdown link on the index page routes to the linked guide content.
- Implemented: `docs/user-guide/index.md` lists all current user guides under `docs/user-guide/`, excluding `getting-started.md` and `index.md`.

## Tests

- `UserGuideContentServiceTests.Normalize_slug_rejects_path_traversal`
- `UserGuideContentServiceTests.GetArticle_loads_markdown_title_and_renders_content`
- `UserGuideContentServiceTests.RenderMarkdown_routes_user_guide_links_to_help_pages`

## User Documentation Impact

- Created `docs/user-guide/getting-started.md`.
- Created `docs/user-guide/index.md` with links to existing user-guide articles.

## Current Implementation

- `NavMenu.razor` shows Getting Started and User Guide under the existing Help section.
- `HelpPage.razor` handles `/help`, `/help/getting-started`, `/help/user-guide`, and `/help/user-guide/{GuideSlug}`.
- `UserGuideContentService` loads markdown from `docs/user-guide`, renders headings, paragraphs, unordered lists, and markdown links, and rewrites guide markdown links to in-app Help routes.
- The Web project copies `docs/user-guide/*.md` into build and publish output.

## Outstanding Tasks

- Add browser-level coverage for Help menu navigation and guide link clicks.
- Consider replacing the lightweight markdown renderer with a full markdown library if guides need tables, images, or complex formatting.
- Consider role-filtering user-guide index links if future guides contain role-specific or sensitive content.

## Change Log

- 2026-08-04: Implemented in-app Getting Started and User Guide Help pages backed by `docs/user-guide` markdown.
