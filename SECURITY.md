# Security policy

Version 0.2.x is the supported release line. Report suspected vulnerabilities privately to the maintainer of the repository or the operator of the affected instance. If the repository host offers private vulnerability reporting, use it. Include the version, affected route, impact and a reproduction with synthetic inputs. Use the public issue tracker for ordinary bugs.

The API accepts unauthenticated requests. Its health endpoint exposes service status, and the Swagger documentation endpoints are limited to Development and Homologation. The public playground is available in every environment. Brazilian document and email checks operate on syntax or check digits and make no external requests. Public CEP, locality, bank and indicator observation lookups use their respective external providers.
