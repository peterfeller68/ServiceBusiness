# User-Guide

Status: Planned | In Progress | Implemented | Deprecated
Owner: Product
Last reviewed: YYYY-MM-DD

## Problem

Users need to have access to the user-guides that are created as part of the development cycle

## Personas

## Requirements
- The Getting Started page shows up under the Help menu
- The User-Guide page shows up under the Help Menu

## User Flows

## UI Expectations
- The Getting Started is a static HTML page located under docs/user-guide
- The User-Guide page is a page with links to all the user-guides created under docs/user-guide

## Data Model Impact

## Authorization Rules

## Acceptance Criteria
- The Help menu item has a Getting Started and User-Guide menu item
- When choosing Getting Started, it displays the contents of docs/getting-started.md
- When choosing User-Guide, it displays a file called index.md, which contains links to all user-guides
- When clicking on a link on the index.md page, it shall display the contents of the link
- Index.md is created with all user-guides under docs/user-guide, with the exception of getting-started.md

## Tests

## User Documentation Impact

## Current Implementation

## Outstanding Tasks

## Change Log
